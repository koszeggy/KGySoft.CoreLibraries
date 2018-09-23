using System;
using System.Diagnostics;

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Represents a measured profiler item.
    /// </summary>
    [DebuggerDisplay("{category}/{operation}: Total: {totalElapsed}; Average: {new System.TimeSpan(totalElapsed.Ticks / calls)}; Calls: {calls}")]
    internal sealed class MeasureItem : IMeasureItem
    {
        #region Fields

        private readonly string category;
        private readonly string operation;
        private readonly object syncRoot = new object();

        private long calls;
        private TimeSpan firstCall;
        private TimeSpan totalElapsed;

        #endregion

        #region Constructors

        internal MeasureItem(string category, string operation)
        {
            this.category = category;
            this.operation = operation;
        }

        #endregion

        #region Properties

        public string Category
        {
            get { return category; }
        }

        public string Operation
        {
            get { return operation; }
        }

        public long NumberOfCalls
        {
            get { return calls; }
        }

        public TimeSpan FirstCall
        {
            get { return firstCall; }
        }

        public TimeSpan TotalElapsed
        {
            get { return totalElapsed; }
        }

        #endregion

        #region Methods

        internal void AddMeasurement(TimeSpan timeSpan)
        {
            lock (syncRoot)
            {
                if (calls == 0L)
                {
                    totalElapsed = timeSpan;
                    firstCall = timeSpan;
                }
                else
                {
                    totalElapsed += timeSpan;
                }

                calls++;
            }
        }

        #endregion
    }
}
