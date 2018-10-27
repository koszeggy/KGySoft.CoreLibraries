using System;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Represents a simple event argument of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the event argument.</typeparam>
    public class EventArgs<T> : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the event data.
        /// </summary>
        public T EventData { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EventArgs{T}"/> class.
        /// </summary>
        /// <param name="arg">The argument.</param>
        public EventArgs(T arg) => EventData = arg;

        #endregion
    }
}
