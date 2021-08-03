#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: HiResTimerElapsedEventArgs.cs
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

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides data for the <see cref="HiResTimer.Elapsed">HiResTimer.Elapsed</see> event.
    /// </summary>
    public class HiResTimerElapsedEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Gets the delay, in milliseconds, of the triggering of the <see cref="HiResTimer.Elapsed">HiResTimer.Elapsed</see> event
        /// compared to when it should have been called.
        /// </summary>
        public float Delay { get; }

        /// <summary>
        /// Gets the number of the fallen out <see cref="HiResTimer.Enabled">HiResTimer.Enabled</see> events since the last invoke.
        /// The value is nonzero if a larger delay occurred than the value of the <see cref="HiResTimer.IgnoreElapsedThreshold">HiResTimer.IgnoreElapsedThreshold</see> property
        /// and thus one or more <see cref="HiResTimer.Elapsed">HiResTimer.Elapsed</see> events were skipped to catch up the timer.
        /// </summary>
        public int Fallouts { get; }

        #endregion

        #region Constructors

        internal HiResTimerElapsedEventArgs(float delay, int fallouts)
        {
            Delay = delay;
            Fallouts = fallouts;
        }

        #endregion
    }
}
