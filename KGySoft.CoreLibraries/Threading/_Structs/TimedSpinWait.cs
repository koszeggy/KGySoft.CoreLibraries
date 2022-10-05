#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TimedSpinWait.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
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
using System.Threading;

using KGySoft.CoreLibraries;
#if !NETFRAMEWORK
using KGySoft.Reflection;
#endif

#endregion

namespace KGySoft.Threading
{
    internal struct TimedSpinWait
    {
        #region Constants

        private const int yieldThreshold = 10;
        private const int maxPower = 12;
        private const int defaultMaxSpinWait = 1 << maxPower;
#if !NET35
        private const int sleep0EveryHowManyYields = 5; 
#endif
        private const int defaultSleep1ThresholdTime = 15;

        #endregion

        #region Fields

        #region Static Fields

#if !NETFRAMEWORK
        private static int? maxSpin;
#endif

        #endregion

        #region Instance Fields

        private int count;
        private long sleep1Timeout;

        #endregion

        #endregion

        #region Properties

#if NETFRAMEWORK
        internal static int MaxSpinWait => defaultMaxSpinWait;
#else
        internal static int MaxSpinWait => maxSpin
            ??= Reflector.TryGetProperty(typeof(Thread), "OptimalMaxSpinWaitsPerSpinIteration", out object? value) && value is int i ? i : defaultMaxSpinWait;
#endif

        #endregion

        #region Methods

        internal void SpinOnce()
        {
            // Neither the .NET Framework, nor the .NET Core implementations are perfect. The Framework implementation doubled SpinOnce calls
            // until up to 4096 and then switched to yielding in every iteration. It was suboptimal because it could cause that two threads
            // just started switching between each other, which was fixed by the .NET Core implementation. However, the Core implementation
            // reduced the SpinOnce times radically by capping the max value by the internal Thread.OptimalMaxSpinWaitsPerSpinIteration, which
            // returns a very low value (around 10 or below), causing that the first Sleep(1), which lasts for at least 15.5 ms by default,
            // occurs way too soon, ruining the performance in many cases.

            // This implementation also uses the interleaved spinning and yielding but instead of using a counter-based threshold for switching
            // to Sleep(1), uses a timeout for it. It is 15 milliseconds by default, which is still shorter than the default Sleep(1) duration.
            // It prevents degrading the performance if the awaited event occurs before one Sleep(1) would finish.

            long timestamp = Stopwatch.GetTimestamp();
            if (count == 0)
                sleep1Timeout = timestamp + TimeHelper.ToStopwatchTicks(defaultSleep1ThresholdTime);

            // Once we step into Sleep(1) phase, count is not updated anymore
            if (timestamp >= sleep1Timeout)
            {
                Thread.Sleep(1);
                return;
            }

            // (count & 1) == 0: interleaving spinning and Yield/Sleep(0) because they return immediately when there are no waiting threads.
            // This can prevent also switching threads between each other if there are more threads calling Yield/Sleep(0) at the same time
            if (count >= yieldThreshold && (count & 1) == 0 || ParallelHelper.IsSingleCoreCpu)
            {
#if NET35
                Thread.Sleep(0);
#else
                int yieldsSoFar = count >= yieldThreshold ? (count - yieldThreshold) >> 1 : count;
                if (yieldsSoFar % sleep0EveryHowManyYields == sleep0EveryHowManyYields - 1)
                    Thread.Sleep(0);
                else
                    Thread.Yield();
#endif
            }
            else
                Thread.SpinWait(count <= maxPower ? Math.Min(1 << count, MaxSpinWait) : MaxSpinWait);

            count = count == Int32.MaxValue ? yieldThreshold : count + 1;
        }

        #endregion
    }
}