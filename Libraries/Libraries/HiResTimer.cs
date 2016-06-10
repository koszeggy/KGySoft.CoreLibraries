using System;
using System.Diagnostics;
using System.Threading;
using KGySoft.Libraries.Resources;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Represents a high resolution timer that allows precise timing even with sub-milliseconds intervals.
    /// The timer executes on a separated high proprity thread.
    /// </summary>
    public class HiResTimer
    {
        /// <summary>
        /// The number of ticks per one millisecond.
        /// </summary>
        private readonly static float tickFrequency = 1000f / Stopwatch.Frequency;

        /// <summary>
        /// Occurs when the <see cref="Interval"/> elapses.
        /// </summary>
        public event EventHandler<HiResTimerElapsedEventArgs> Elapsed;

        private float interval;
        private float ignoreElapsedThreshold = Single.PositiveInfinity;
        private volatile bool isRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="HiResTimer"/> class with 1ms interval.
        /// </summary>
        public HiResTimer() : this(1f)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HiResTimer"/> class with a specified <paramref name="interval"/>.
        /// </summary>
        /// <param name="interval">The time, in millisecods, between events. Value must be non-negative. Fractional values are allowed.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval"/> is negative or <see cref="Single.NaN"/>.</exception>
        public HiResTimer(float interval)
        {
            if (interval < 0f || Single.IsNaN(interval))
                throw new ArgumentOutOfRangeException(nameof(interval), Res.Get(Res.ArgumentOutOfRange));
            this.interval = interval;
        }

        /// <summary>
        /// Gets or sets the interval, in milliseconds, before <see cref="Elapsed"/> event is triggered.
        /// Fractional values are allowed, too. When zero, the <see cref="Elapsed"/> event is triggered as often as possible.
        /// </summary>
        /// <value>
        /// The interval in milliseconds. For example, 1000 represents one second and 0.001 represents one microsecond.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is negative or <see cref="Single.NaN"/>.</exception>
        public float Interval
        {
            get
            {
                return Interlocked.CompareExchange(
                    ref interval, -1f, -1f);
            }
            set
            {
                if (value < 0f || Single.IsNaN(value))
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
                Interlocked.Exchange(
                    ref interval, value);
            }
        }

        /// <summary>
        /// Gets or sets a threshold value, in milliseconds, to ignore an <see cref="Elapsed"/> event (and thus trying to catch up the timer)
        /// if the next invoke lates by the given value. Value must not be zero but fractionals are allowed.
        /// </summary>
        /// <value>
        /// The ignore elapsed threshold.
        /// </value>
        /// <remarks>
        /// <note>
        /// If the value of this property is too low (smaller than the execution time of the <see cref="Elapsed"/> event), it may
        /// cause that the <see cref="Elapsed"/> event is never triggered again.
        /// </note>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is zero or negative or <see cref="Single.NaN"/>.</exception>
        public float IgnoreElapsedThreshold
        {
            get
            {
                return Interlocked.CompareExchange(
                    ref ignoreElapsedThreshold, -1f, -1f);
            }
            set
            {
                if (value <= 0f || Single.IsNaN(value))
                    throw new ArgumentOutOfRangeException(nameof(value), Res.Get(Res.ArgumentOutOfRange));
                Interlocked.Exchange(
                    ref ignoreElapsedThreshold, value);
            }
        }

        /// <summary>
        /// Gets or sets whether the <see cref="Elapsed"/> event should be triggered.
        /// </summary>
        /// <value>
        /// <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            set
            {
                if (value)
                    Start();
                else
                    Stop();
            }
            get
            {
                return isRunning;
            }
        }

        /// <summary>
        /// Starts raising the <see cref="Elapsed"/> event by enabling the timer.
        /// </summary>
        public void Start()
        {
            if (isRunning)
                return;

            isRunning = true;
            Thread thread = new Thread(ExecuteTimer);
            thread.Priority = ThreadPriority.Highest;
            thread.Start();
        }

        /// <summary>
        /// Stops raising the <see cref="Elapsed"/> event by disabling the timer.
        /// </summary>
        public void Stop()
        {
            isRunning = false;
        }

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

                    if (diff < 1d)
                        Thread.SpinWait(10);
                    else if (diff < 5d)
                        Thread.SpinWait(100);
                    else if (diff < 15d)
                        Thread.Sleep(1);
                    else
                    {
                        // if we have a larger time to wait, we check if the interval has been changed in the meantime
                        Thread.Sleep(10);
                        float newInterval = Interlocked.CompareExchange(ref interval, -1f, -1f);
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
                    fallouts++;
                    continue;
                }

                Elapsed?.Invoke(this, new HiResTimerElapsedEventArgs(delay, fallouts));
                fallouts = 0;

                // restarting the timer in every hour to prevent precision problems
                if (stopwatch.Elapsed.TotalHours >= 1d)
                {
                    stopwatch.Restart();
                    nextTrigger = 0f;
                }
            }

            stopwatch.Stop();
        }

        private static float ElapsedHiRes(Stopwatch stopwatch)
        {
            return stopwatch.ElapsedTicks * tickFrequency;
        }
    }
}