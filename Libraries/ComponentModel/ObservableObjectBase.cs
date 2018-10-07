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
using System.Runtime.CompilerServices;
using System.Threading;

using KGySoft.Annotations;

#endregion

namespace KGySoft.ComponentModel
{
    /// <summary>
    /// Provides a base class for business objects or ViewModel classes, which can notify their consumer about property changes.
    /// </summary>
    /// <seealso cref="INotifyPropertyChanged" />
    /// <seealso cref="PersistableObjectBase" />
    public abstract class ObservableObjectBase : INotifyPropertyChanged, IDisposable
    {
        #region Fields

        private PropertyChangedEventHandler propertyChanged;
        private int suspendCounter;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => propertyChanged += value;
            remove => propertyChanged -= value;
        }

        #endregion

        #region Methods

        #region Public Methods

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

        #region Protected Methods

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property. This parameter is optional.
        /// <br/>Default value: The name of the caller member.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(
#if NET35 || NET40
            string propertyName
#else
            [CallerMemberName] string propertyName = null
#endif
            )
        {
            if (suspendCounter <= 0)
                propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Suspends the raising of the <see cref="PropertyChanged"/> event until <see cref="ResumePropertyChanged">ResumePropertyChanged</see>
        /// method is called. Supports nested calls.
        /// </summary>
        protected void SuspendPropertyChanged() => Interlocked.Increment(ref suspendCounter);

        /// <summary>
        /// Resumes the raising of the <see cref="PropertyChanged"/> event suspended by the <see cref="SuspendPropertyChanged">SuspendPropertyChanged</see> method.
        /// </summary>
        protected void ResumePropertyChanged() => Interlocked.Decrement(ref suspendCounter);

        /// <summary>
        /// Releases the resources held by this instance.
        /// The base implementation removes the subscribers of the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) => propertyChanged = null;

#endregion

#endregion
    }
}
