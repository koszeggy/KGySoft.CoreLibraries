using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
#if !NET35
using System.Dynamic;
#endif
using System.Linq;
using System.Text;

namespace KGySoft.ComponentModel
{
    internal sealed class BindingState :
#if !NET35
        DynamicObject, 
#endif
        ICommandState, INotifyPropertyChanged
    {
        private readonly Dictionary<string, object> stateProperties = new Dictionary<string, object> { [nameof(Enabled)] = true };

        public BindingState(IDictionary<string, object> configuration)
        {
            if (configuration == null)
                return;

            foreach (var state in configuration)
            {
                if (state.Key == nameof(Enabled) && !(state.Value is bool))
                    throw new ArgumentException($"'{Enabled}' state must have a bool value", nameof(configuration));
                stateProperties[state.Key] = state.Value;
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            lock (stateProperties)
                return new Dictionary<string, object>(stateProperties).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            // not calling PropertyChanged because a removed property has no effect on update
            lock (stateProperties)
            {
                stateProperties.Clear();
                Enabled = true; // this may call PropertyChanged though
            }
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            lock (stateProperties)
                return ((ICollection<KeyValuePair<string, object>>)stateProperties).Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            lock (stateProperties)
                ((ICollection<KeyValuePair<string, object>>)stateProperties).CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            lock (stateProperties)
                return ((ICollection<KeyValuePair<string, object>>)stateProperties).Remove(item);
        }

        public int Count
        {
            get
            {
                lock (stateProperties)
                    return stateProperties.Count;
            }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

        public bool ContainsKey(string key)
        {
            lock (stateProperties)
                return stateProperties.ContainsKey(key);
        }

        public void Add(string key, object value)
        {
            lock (stateProperties)
            {
                if (stateProperties.ContainsKey(key))
                    throw new ArgumentException("An item with the same key has already been added.", nameof(key));
                //throw new ArgumentException(Res.Get(Res.DuplicateKey), nameof(key));

                stateProperties.Add(key, value);
                OnPropertyChanged(key);
            }
        }

        public bool Remove(string key)
        {
            // not calling PropertyChanged because a removed property has no effect on update
            lock (stateProperties)
                return stateProperties.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            lock (stateProperties)
                return stateProperties.TryGetValue(key, out value);
        }

        public object this[string key]
        {
            get
            {
                lock (stateProperties)
                    return stateProperties[key];
            }
            set
            {
                if (key == nameof(Enabled) && !(value is bool))
                    throw new ArgumentException($"'{Enabled}' state must have a bool value");

                lock (stateProperties)
                {
                    bool differs = !stateProperties.TryGetValue(key, out object oldValue) || !Equals(oldValue, value);
                    stateProperties[key] = value;
                    if (differs)
                        OnPropertyChanged(key);
                }
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                lock (stateProperties)
                    return stateProperties.Keys.ToArray();
            }
        }

        public ICollection<object> Values
        {
            get
            {
                lock (stateProperties)
                    return stateProperties.Values.ToArray();
            }
        }

        public bool Enabled
        {
            get => (bool)this[nameof(Enabled)];
            set => this[nameof(Enabled)] = value;
        }

#if !NET35
        public dynamic AsDynamic => this;
#endif

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

#if !NET35
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];
            return true;
        }
#endif
    }
}
