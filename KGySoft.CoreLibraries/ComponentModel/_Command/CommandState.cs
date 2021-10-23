#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CommandState.cs
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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
#if !NET35
using System.Dynamic;
#endif

using KGySoft.Collections;
using KGySoft.CoreLibraries;
using KGySoft.Reflection;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Represents the states of a command for a specific command binding.
    /// <br/>See the <see cref="ICommandState"/> interface for details and the <strong>Remarks</strong> section of <see cref="ICommand"/> for some examples.
    /// </summary>
    /// <seealso cref="ICommand" />
    /// <seealso cref="ICommandState" />
    /// <seealso cref="ICommandStateUpdater" />
    /// <seealso cref="ICommandBinding" />
    public sealed class CommandState :
#if !NET35
        DynamicObject,
        ITypedList, // so a binding will not treat the type as a list of Key and Value properties (because the CommandState implements IDictionary<string, object>)
#if !NET40
        IReadOnlyDictionary<string, object?>,
#endif
#endif
        ICommandState,
        ICustomTypeDescriptor // so the dynamic properties can be reflected as normal ones (eg. in a property grid)
    {
        #region CommandStatePropertyDescriptor class

        private class CommandStatePropertyDescriptor : PropertyDescriptor
        {
            #region Properties

            public override bool IsReadOnly => false;
            public override Type ComponentType => typeof(CommandState);
            public override Type PropertyType { get; }

            #endregion

            #region Constructors

            internal CommandStatePropertyDescriptor(string name, Type type) : base(name, null) => PropertyType = type;

            #endregion

            #region Methods

            public override bool CanResetValue(object component) => Name == nameof(Enabled);
            public override object? GetValue(object? component) => ((CommandState)component!)[Name];
            public override void ResetValue(object component) => ((CommandState)component).Enabled = true;
            public override void SetValue(object? component, object? value) => ((CommandState)component!)[Name] = value;
            public override bool ShouldSerializeValue(object component) => Name != nameof(Enabled) || !((CommandState)component).Enabled;

            #endregion
        }

        #endregion

        #region Fields

        private readonly ThreadSafeDictionary<string, object?> stateProperties = new ThreadSafeDictionary<string, object?>(StringSegmentComparer.Ordinal) { [nameof(Enabled)] = true };

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a state entry value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Properties and Indexers

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets the number of elements contained in the <see cref="CommandState" />.
        /// </summary>
        public int Count => stateProperties.Count;

        /// <summary>
        /// Gets or sets whether the command is enabled in the current binding.
        /// <br/>Default value: <see langword="true"/>.
        /// </summary>
        /// <value><see langword="true"/>&#160;if the command enabled and can be executed; otherwise, <see langword="false" />.</value>
        public bool Enabled
        {
            get => (bool)this[nameof(Enabled)]!;
            set => this[nameof(Enabled)] = value;
        }

#if !NET35
        /// <summary>
        /// Gets the state as a dynamic object so the states can be set by simple property setting syntax.
        /// </summary>
        public dynamic AsDynamic => this;
#endif

        #endregion

        #region Explicitly Implemented Interface Properties

        bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;
        ICollection<string> IDictionary<string, object?>.Keys => stateProperties.Keys;
        ICollection<object?> IDictionary<string, object?>.Values => stateProperties.Values;
#if !(NET35 || NET40)
        IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => stateProperties.Keys;
        IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => stateProperties.Values;
#endif

        #endregion

        #endregion

        #region Indexers

        /// <summary>
        /// Gets or sets the state value with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the state value to get or set.</param>
        public object? this[string key]
        {
            get => stateProperties[key];
            set
            {
                if (key == nameof(Enabled) && value is not bool)
                    Throw.ArgumentException(Argument.value, Res.ComponentModelEnabledMustBeBool);

                bool differs = true;
                stateProperties.AddOrUpdate(key, value, (_, oldValue) =>
                {
                    if (Equals(oldValue, value))
                        differs = false;
                    return value;
                });

                if (differs)
                    OnPropertyChanged(key);
            }
        }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandState"/> class from an initial configuration if provided.
        /// </summary>
        /// <param name="initialConfiguration">The initial configuration to use for initializing this <see cref="CommandState"/> instance. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="initialConfiguration"/> contains a non-<see cref="bool">bool</see>&#160;<c>Enabled</c> entry.</exception>
        public CommandState(IDictionary<string, object?>? initialConfiguration = null)
        {
            if (initialConfiguration == null)
                return;

            foreach (KeyValuePair<string, object?> state in initialConfiguration)
            {
                if (state.Key == nameof(Enabled) && state.Value is not bool)
                    Throw.ArgumentException(Argument.initialConfiguration, Res.ComponentModelEnabledMustBeBool);
                stateProperties[state.Key] = state.Value;
            }
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
        /// <remarks>
        /// <note>The returned enumerator supports the <see cref="IEnumerator.Reset">IEnumerator.Reset</see> method.</note>
        /// </remarks>
        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => stateProperties.GetEnumerator();

        /// <summary>
        /// Determines whether the <see cref="CommandState" /> contains an element with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="CommandState" />.</param>
        /// <returns><see langword="true"/>&#160;if the <see cref="CommandState" /> contains an element with the key; otherwise, <see langword="false" />.</returns>
        public bool ContainsKey(string key) => stateProperties.ContainsKey(key);

        /// <summary>
        /// Adds a state element with the provided key and value to the <see cref="CommandState" />.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="ArgumentException">An item with the same key has already been added</exception>
        public void Add(string key, object? value)
        {
            stateProperties.Add(key, value);
            OnPropertyChanged(key);
        }

        /// <summary>
        /// Gets the state element associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">When this method returns, the value associated with the specified <paramref name="key"/>, if the key is found; otherwise, the default value for the type of the <paramref name="value" /> parameter. This parameter is passed uninitialized.</param>
        /// <returns><see langword="true"/>&#160;if the <see cref="CommandState"/> contains an element with the specified <paramref name="key"/>; otherwise, <see langword="false" />.
        /// </returns>
        public bool TryGetValue(string key, out object? value) => stateProperties.TryGetValue(key, out value);

#if !NET35

        /// <summary>
        /// Sets the <paramref name="value"/> of a state specified by the <see cref="SetMemberBinder.Name"/> property.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation.</param>
        /// <param name="value">The value of the state to set.</param>
        /// <returns>This method always return <see langword="true"/>.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool TrySetMember(SetMemberBinder binder, object? value)
        {
            if (binder == null!)
                Throw.ArgumentNullException(Argument.binder);
            this[binder.Name] = value;
            return true;
        }

        /// <summary>
        /// Gets the state element associated with the specified <see cref="GetMemberBinder.Name"/>.
        /// </summary>
        /// <param name="binder">Provides information about the object that called the dynamic operation.</param>
        /// <param name="result">The value associated with the specified <see cref="GetMemberBinder.Name"/>.</param>
        /// <returns>This method always return <see langword="true"/>.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (binder == null!)
                Throw.ArgumentNullException(Argument.binder);
            result = this[binder.Name];
            return true;
        }
#endif

        #endregion

        #region Private Methods

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private PropertyDescriptorCollection GetProperties()
        {
            var result = new PropertyDescriptorCollection(null);
            foreach (KeyValuePair<string, object?> property in stateProperties)
                result.Add(new CommandStatePropertyDescriptor(property.Key, property.Value?.GetType() ?? Reflector.ObjectType));
            return result;
        }

        #endregion

        #region Explicitly Implemented Interface Methods

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
            => ((ICollection<KeyValuePair<string, object?>>)stateProperties).Contains(item);
        
        void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<string, object?>>)stateProperties).CopyTo(array, arrayIndex);

        void ICollection<KeyValuePair<string, object?>>.Clear()
        {
            // not calling PropertyChanged because a removed property has no effect on update
            stateProperties.Clear();
            Enabled = true; // this may call PropertyChanged though
        }

        bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
        {
            if (item.Key == nameof(Enabled))
                return false;
            return ((ICollection<KeyValuePair<string, object?>>)stateProperties).Remove(item);
        }

        bool IDictionary<string, object?>.Remove(string key)
        {
            if (key == nameof(Enabled))
                return false;

            // not calling PropertyChanged because a removed property has no effect on update
            return stateProperties.TryRemove(key);
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes() => new AttributeCollection(null);
        string ICustomTypeDescriptor.GetClassName() => nameof(CommandState);
        string ICustomTypeDescriptor.GetComponentName() => ToString()!;
        TypeConverter ICustomTypeDescriptor.GetConverter() => new TypeConverter();
        EventDescriptor? ICustomTypeDescriptor.GetDefaultEvent() => null;
        PropertyDescriptor? ICustomTypeDescriptor.GetDefaultProperty() => null;
        object? ICustomTypeDescriptor.GetEditor(Type editorBaseType) => null;
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents() => new EventDescriptorCollection(null);
        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[]? attributes) => new EventDescriptorCollection(null);
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties() => GetProperties();
        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[]? attributes) => attributes.IsNullOrEmpty() ? GetProperties() : new PropertyDescriptorCollection(null);
        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor? pd) => this;

#if !NET35
        string ITypedList.GetListName(PropertyDescriptor[] listAccessors) => ToString()!;
        PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors) => GetProperties();
#endif

        #endregion

        #endregion
    }
}
