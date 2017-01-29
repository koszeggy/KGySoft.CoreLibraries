using System;
using System.ComponentModel;

namespace KGySoft.Libraries.Resources
{
    /// <summary>
    /// Provides data for the <see cref="DynamicResourceManager.AutoSaveError"/> event.
    /// </summary>
    public sealed class AutoSaveErrorEventArgs : HandledEventArgs
    {
        private readonly Exception exception;

        /// <summary>
        /// Gets the <see cref="System.Exception"/> that occurred on auto saving.
        /// </summary>
        public Exception Exception => exception;

        internal AutoSaveErrorEventArgs(Exception exception) : base(false)
        {
            this.exception = exception;
        }
    }
}
