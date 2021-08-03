#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MeasureItem.cs
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

namespace KGySoft.Diagnostics
{
    /// <summary>
    /// Represents a measured profiler item.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{category}/{operation}: Total: {totalElapsed}; Average: {new System.TimeSpan(totalElapsed.Ticks / calls)}; Calls: {calls}")]
    internal sealed class MeasureItem : IMeasureItem
    {

        #region Fields

        private readonly string category;
        private readonly string operation;
        private long calls;

        private readonly object syncRoot = new object();
        private TimeSpan firstCall;
        private TimeSpan totalElapsed;

        #endregion

        #region Properties

        public string Category => category;
        public string Operation => operation;
        public long NumberOfCalls => calls;
        public TimeSpan FirstCall => firstCall;
        public TimeSpan TotalTime => totalElapsed;

        #endregion

        #region Constructors

        internal MeasureItem(string category, string operation)
        {
            this.category = category;
            this.operation = operation;
        }

        #endregion

        #region Methods

        public override string ToString() => calls == 0 ? base.ToString()! : Res.ProfilerMeasureItemToString(Category, Operation, new TimeSpan(TotalTime.Ticks / NumberOfCalls), FirstCall, TotalTime, NumberOfCalls);

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
                    totalElapsed += timeSpan;

                calls += 1;
            }
        }

        #endregion
    }
}
