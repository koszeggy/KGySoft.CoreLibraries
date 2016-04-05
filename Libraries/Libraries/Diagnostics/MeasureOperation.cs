using System;
using System.Diagnostics;

namespace KGySoft.Libraries.Diagnostics
{
    /// <summary>
    /// Represents a measured profiler operation
    /// </summary>
    internal sealed class MeasureOperation : IDisposable
    {
        #region Fields

        private MeasureItem item;
        private readonly Stopwatch stopwatch = new Stopwatch();

        #endregion

        #region Constructors

        public MeasureOperation(MeasureItem item)
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
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            item.AddMeasurement(stopwatch.Elapsed);
            item = null;
        }

        #endregion
    }
}
