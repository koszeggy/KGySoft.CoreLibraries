#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: TestProgress.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#nullable enable

#region Usings

using System;

using KGySoft.Threading;

#endregion

namespace KGySoft.CoreLibraries.UnitTests.Threading
{
    internal class TestProgress : IAsyncProgress
    {
        #region Fields

        private readonly object syncRoot = new object();
        private (string? Op, int Max, int Value) current;

        #endregion

        #region Properties

        internal AsyncProgress<string?> Current
        {
            get
            {
                lock (syncRoot)
                    return new AsyncProgress<string?>(current.Op, current.Max, current.Value);
            }
        }

        #endregion

        #region Methods

        public void Report<T>(AsyncProgress<T> progress)
        {
            lock (syncRoot)
                current = (progress.OperationType?.ToString(), progress.MaximumValue, progress.CurrentValue);
        }

        public void New<T>(T operationType, int maximumValue, int currentValue)
            => Report(new AsyncProgress<T>(operationType, maximumValue, currentValue));

        public void Increment()
        {
            lock (syncRoot)
                current.Value++;
        }

        public void SetProgressValue(int value)
        {
            lock (syncRoot)
                current.Value = value;
        }

        public void Complete()
        {
            lock (syncRoot)
                current.Value = current.Max;
        }

        #endregion
    }
}
