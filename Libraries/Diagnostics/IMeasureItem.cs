using System;

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Represents a measurement item that is managed by the <see cref="Profiler"/> class.
    /// </summary>
    public interface IMeasureItem
    {
        /// <summary>
        /// Gets the category name of the measurement item.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Gets the operation name of the measurement item.
        /// </summary>
        string Operation { get; }

        /// <summary>
        /// Gets the number of calls of the current operation.
        /// </summary>
        long NumberOfCalls { get; }

        /// <summary>
        /// Gets the duration of the first call of the current operation.
        /// </summary>
        TimeSpan FirstCall { get; }

        /// <summary>
        /// Gets the total duration of the current operation.
        /// </summary>
        TimeSpan TotalElapsed { get; }
    }
}
