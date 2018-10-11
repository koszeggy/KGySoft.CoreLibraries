#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObservableObjectBase.cs
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
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Threading;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a base class for business object model classes, which can notify their consumer about property changes.
    /// </summary>
    /// <seealso cref="INotifyPropertyChanged" />
    /// <seealso cref="PersistableObjectBase" />
    public abstract class ObservableObjectBase : INotifyPropertyChanging, INotifyPropertyChanged, IDisposable
    {
        #region MissingProperty class

        [Serializable]
        private sealed class MissingPropertyReference : IObjectReference
        {
            public object GetRealObject(StreamingContext context) => Value;
            internal static MissingPropertyReference Value { get; } = new MissingPropertyReference();
            public override string ToString() => Res.Get(Res.MissingPropertyReference);
        }

        #endregion

        #region Fields

        private PropertyChangingEventHandler propertyChanging;
        private PropertyChangedEventHandler propertyChanged;
        private int suspendCounter;
        private bool isModified;

        #endregion

        #region Properties

        #region Static Properties

        /// <summary>
        /// Represents the value of a missing property value. Can be returned in <see cref="PropertyChangingExtendedEventArgs"/> and <see cref="PropertyChangedExtendedEventArgs"/>
        /// by <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events if the stored value of the property has just been created and had no previous value, or when
        /// a property has been removed from an inner storage.
        /// </summary>
        /// <remarks><note>Reading the property when it has no value may return a default value or can cause to recreate a value.</note></remarks>
        public static object MissingProperty { get; } = MissingPropertyReference.Value;

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets whether this instance has been modified.
        /// Modified state can be set by the <see cref="SetModified">SetModified</see> method.
        /// </summary>
        public bool IsModified => isModified;

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes. The actual type of the event argument is <see cref="PropertyChangingExtendedEventArgs"/>. 
        /// </summary>
        /// <remarks>
        /// <note>The <see cref="PropertyChanging"/> event uses the <see cref="PropertyChangingEventHandler"/> delegate in order to consumers, which rely on the conventional property
        /// changing notifications can use it in a compatible way. To get the old value in an event handler you can cast the argument to <see cref="PropertyChangingExtendedEventArgs"/>
        /// or call the <see cref="EventArgsExtensions.TryGetPropertyValue">TryGetPropertyValue</see> extension method on it.</note>
        /// <note>The value change cannot be canceled by subscribing this event. To disallow setting a property derive from the <see cref="PersistableObjectBase"/>
        /// and override its <see cref="PersistableObjectBase.CanSetProperty">CanSetProperty</see> instead. And for property validation use the <see cref="ValidatingObjectBase"/> class.</note>
        /// </remarks>
        public event PropertyChangingEventHandler PropertyChanging
        {
            add => propertyChanging += value;
            remove => propertyChanging -= value;
        }

        /// <summary>
        /// Occurs when a property value changed. The actual type of the event argument is <see cref="PropertyChangedExtendedEventArgs"/>. 
        /// </summary>
        /// <remarks>
        /// <note>The <see cref="PropertyChanged"/> event uses the <see cref="PropertyChangedEventHandler"/> delegate in order to consumers, which rely on the conventional property
        /// changed notifications can use it in a compatible way. To get the old value in an event handler you can cast the argument to <see cref="PropertyChangedExtendedEventArgs"/>
        /// or call the <see cref="EventArgsExtensions.TryGetOldPropertyValue">TryGetOldPropertyValue</see> and <see cref="EventArgsExtensions.TryGetNewPropertyValue">TryGetNewPropertyValue</see> extension method on it.</note>
        /// </remarks>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => propertyChanged += value;
            remove => propertyChanged -= value;
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Sets the modified state of this <see cref="ObservableObjectBase"/> instance represented by the <see cref="IsModified"/> property.
        /// </summary>
        /// <param name="value"><see langword="true"/> to mark the object as modified; <see langword="false"/> to mark it unmodified.</param>
        public void SetModified(bool value)
        {
            if (isModified == value)
                return;

            OnPropertyChanging(new PropertyChangingExtendedEventArgs(isModified, nameof(IsModified)));
            isModified = value;
            OnPropertyChanged(new PropertyChangedExtendedEventArgs(!value, value, nameof(IsModified)));
        }

        /// <summary>
        /// Releases the resources held by this instance.
        /// The base implementation removes the subscribers of the <see cref="PropertyChanged"/> event.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected Internal Methods

        /// <summary>
        /// Raises the <see cref="PropertyChanging" /> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangingExtendedEventArgs" /> instance containing the event data.</param>
        protected internal virtual void OnPropertyChanging(PropertyChangingExtendedEventArgs e)
        {
            if (suspendCounter <= 0)
                propertyChanging?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedExtendedEventArgs" /> instance containing the event data.</param>
        protected internal virtual void OnPropertyChanged(PropertyChangedExtendedEventArgs e)
        {
            if (suspendCounter <= 0)
                propertyChanged?.Invoke(this, e);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Suspends the raising of the <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events until <see cref="ResumeChangeEvents">ResumeChangeEvents</see>
        /// method is called. Supports nested calls.
        /// </summary>
        protected void SuspendChangeEvents() => Interlocked.Increment(ref suspendCounter);

        /// <summary>
        /// Resumes the raising of the <see cref="PropertyChanging"/> and <see cref="PropertyChanged"/> events suspended by the <see cref="SuspendChangeEvents">SuspendChangeEvents</see> method.
        /// </summary>
        protected void ResumeChangeEvents() => Interlocked.Decrement(ref suspendCounter);

        /// <summary>
        /// Releases the resources held by this instance.
        /// The base implementation removes the subscribers of the <see cref="PropertyChanged"/> and <see cref="PropertyChanging"/> events.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            propertyChanging = null;
            propertyChanged = null;
        }

        #endregion

        #endregion
    }
}
