using System;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Provides data for the <see cref="HiResTimer.Elapsed"/> event.
    /// </summary>
    public class HiResTimerElapsedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the delay, in milliseconds, of the triggering of the <see cref="HiResTimer.Elapsed"/> event
        /// compared to when it should have been called.
        /// </summary>
        public float Delay { get; }

        /// <summary>
        /// Gets the number of the fallen out <see cref="HiResTimer.Enabled"/> events since the last invoke.
        /// The value is nonzero if the delay <see cref="HiResTimer.IgnoreElapsedThreshold"/> the delais smaller enough and the delay
        /// </summary>
        /// <value>
        /// The fallouts.
        /// </value>
        public int Fallouts { get; }

        internal HiResTimerElapsedEventArgs(float delay, int fallouts)
        {
            Delay = delay;
            Fallouts = fallouts;
        }
    }
}