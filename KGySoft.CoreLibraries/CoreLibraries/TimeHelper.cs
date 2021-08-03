#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TimeHelper.cs
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
using System.Diagnostics; 

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// A helper class to get a time stamp faster than UtcNow if possible on current platform.
    /// NOTE: Do not use if 15 ms accuracy is not enough!
    /// </summary>
    internal static class TimeHelper
    {
        #region Fields

        private static readonly long stopwatchTicksPerMillisecond = Stopwatch.Frequency / 1000;

        #endregion

        #region Methods

        internal static long ToStopwatchTicks(int milliseconds) => milliseconds * stopwatchTicksPerMillisecond;

        internal static long GetTimeStamp() =>
#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP2_0
            // If high resolution is enabled, this can be 3.2 times faster than UtcNow
            Stopwatch.GetTimestamp();
#else
            // On .NET 5 this is 8.1 times faster than UtcNow, though it changes in every 15 ms or so.
            Environment.TickCount64;
#endif

        internal static long GetInterval(int milliseconds) =>
#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP2_0
            ToStopwatchTicks(milliseconds);
#else
            milliseconds;
#endif

        internal static long GetInterval(TimeSpan timeSpan) =>
#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP2_0
            timeSpan.Ticks * stopwatchTicksPerMillisecond / TimeSpan.TicksPerMillisecond;
#else
            timeSpan.Ticks / TimeSpan.TicksPerMillisecond;
#endif

        internal static TimeSpan GetTimeSpan(long interval) =>
#if NETFRAMEWORK || NETSTANDARD || NETCOREAPP2_0
            new TimeSpan(interval * TimeSpan.TicksPerMillisecond / stopwatchTicksPerMillisecond);
#else
            new TimeSpan(interval * TimeSpan.TicksPerMillisecond);
#endif

        #endregion
    }
}
