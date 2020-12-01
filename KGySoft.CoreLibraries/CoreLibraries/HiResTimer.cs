#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: HiResTimer.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
using System.Diagnostics;
using System.Threading;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Represents a high resolution timer that allows precise timing even with sub-milliseconds intervals.
    /// The timer executes on a separated high priority thread.
    /// </summary>
    public class HiResTimer
    {
        #region Fields

        #region Static Fields

        /// <summary>
        /// The number of ticks per one millisecond.
        /// </summary>
        private static readonly float tickFrequency = 1000f / Stopwatch.Frequency;

        #endregion

        #region Instance Fields

        private float interval;
        private float ignoreElapsedThreshold = Single.PositiveInfinity;
        private volatile bool isRunning;

        #endregion

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the <see cref="Interval"/> elapses.
        /// </summary>
        public event EventHandler<HiResTimerElapsedEventArgs>? Elapsed;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the interval, in milliseconds, before <see cref="Elapsed"/> event is triggered.
        /// Fractional values are allowed, too. When zero, the <see cref="Elapsed"/> event is triggered as often as possible.
        /// <br/>Default value: <c>1.0</c>, if initialized by the default constructor; otherwise, as specified in the constructor.
        /// </summary>
        /// <value>
        /// The interval in milliseconds. For example, <c>1000</c> represents one second and <c>0.001</c> represents one microsecond.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative or <see cref="Single.NaN"/>.</exception>
        public float Interval
        {
            get => Interlocked.CompareExchange(ref interval, -1f, -1f);
            set
            {
                if (value < 0f || Single.IsNaN(value))
                    Throw.ArgumentOutOfRangeException(Argument.value);
                Interlocked.Exchange(ref interval, value);
            }
        }

        /// <summary>
        /// Gets or sets a threshold value, in milliseconds, to ignore an <see cref="Elapsed"/> event (and thus trying to catch up the timer)
        /// if the next invoke is late by the given value. Value must not be zero but fractions are allowed.
        /// <br/>Default value: <c>+∞</c>.
        /// </summary>
        /// <remarks>
        /// <note>
        /// If the value of this property is too low (smaller than the execution time of the <see cref="Elapsed"/> event), it may
        /// cause that the <see cref="Elapsed"/> event is never triggered again.
        /// </note>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero or negative or <see cref="Single.NaN"/>.</exception>
        public float IgnoreElapsedThreshold
        {
            get => Interlocked.CompareExchange(ref ignoreElapsedThreshold, -1f, -1f);
            set
            {
                if (value <= 0f || Single.IsNaN(value))
                    Throw.ArgumentOutOfRangeException(Argument.value);
                Interlocked.Exchange(ref ignoreElapsedThreshold, value);
            }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="Elapsed"/> event should be triggered.
        /// <br/>Default value: <see langword="false"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/>&#160;if enabled; otherwise, <see langword="false"/>.
        /// </value>
        public bool Enabled
        {
            get => isRunning;
            set
            {
                if (value)
                    Start();
                else
                    Stop();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HiResTimer"/> class with 1ms interval.
        /// </summary>
        public HiResTimer() : this(1f)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HiResTimer"/> class with a specified <paramref name="interval"/>.
        /// </summary>
        /// <param name="interval">The time, in milliseconds, between events. Value must be non-negative. Fractional values are allowed.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is negative or <see cref="Single.NaN"/>.</exception>
        public HiResTimer(float interval)
        {
            if (interval < 0f || Single.IsNaN(interval))
                Throw.ArgumentOutOfRangeException(Argument.interval);
            this.interval = interval;
        }

        #endregion

        #region Methods

        #region Static Methods

        private static float ElapsedHiRes(Stopwatch stopwatch) => stopwatch.ElapsedTicks * tickFrequency;

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Starts raising the <see cref="Elapsed"/> event by enabling the timer.
        /// </summary>
        public void Start()
        {
            if (isRunning)
                return;

            isRunning = true;
            Thread thread = new Thread(ExecuteTimer) { Priority = ThreadPriority.Highest };
            thread.Start();
        }

        /// <summary>
        /// Stops raising the <see cref="Elapsed"/> event by disabling the timer.
        /// </summary>
        public void Stop() => isRunning = false;

        #endregion

        #region Private Methods

        private void ExecuteTimer()
        {
            int fallouts = 0;
            float nextTrigger = 0f;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (isRunning)
            {
                float intervalLocal = Interlocked.CompareExchange(ref interval, -1f, -1f);
                nextTrigger += intervalLocal;
                float elapsed;

                while (true)
                {
                    elapsed = ElapsedHiRes(stopwatch);
                    float diff = nextTrigger - elapsed;
                    if (diff <= 0f)
                        break;

                    if (diff < 1f)
                        Thread.SpinWait(10);
                    else if (diff < 5f)
                        Thread.SpinWait(100);
                    else if (diff < 15f)
                        Thread.Sleep(1);
                    else
                    {
                        // if we have a larger time to wait, we check if the interval has been changed in the meantime
                        Thread.Sleep(10);
                        float newInterval = Interlocked.CompareExchange(ref interval, -1f, -1f);

                        // ReSharper disable once CompareOfFloatsByEqualityOperator
                        if (intervalLocal != newInterval)
                        {
                            nextTrigger += newInterval - intervalLocal;
                            intervalLocal = newInterval;
                        }
                    }

                    if (!isRunning)
                        return;
                }


                float delay = elapsed - nextTrigger;
                if (delay >= Interlocked.CompareExchange(ref ignoreElapsedThreshold, -1f, -1f))
                {
                    fallouts += 1;
                    continue;
                }

                Elapsed?.Invoke(this, new HiResTimerElapsedEventArgs(delay, fallouts));
                fallouts = 0;

                // restarting the timer in every hour to prevent precision problems
                if (stopwatch.Elapsed.TotalHours >= 1d)
                {
#if NET35
                    stopwatch.Reset();
                    stopwatch.Start();
#else
                    stopwatch.Restart();
#endif


                    nextTrigger = 0f;
                }
            }

            stopwatch.Stop();
        }

        #endregion

        #endregion

        #endregion
    }
}
