#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MeasureOperation.cs
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
    /// Represents a measured profiler operation
    /// </summary>
    internal sealed class MeasureOperation : IDisposable
    {
        #region Fields

        private readonly Stopwatch stopwatch = new Stopwatch();

        private MeasureItem? item;

        #endregion

        #region Constructors

        internal MeasureOperation(MeasureItem item)
        {
            this.item = item;
            stopwatch.Start();
        }

        #endregion

        #region Methods

        public void Dispose()
        {
            stopwatch.Stop();

            if (item == null)
                Throw.ObjectDisposedException(GetType().Name);

            item.AddMeasurement(stopwatch.Elapsed);
            item = null;
        }

        #endregion
    }
}
