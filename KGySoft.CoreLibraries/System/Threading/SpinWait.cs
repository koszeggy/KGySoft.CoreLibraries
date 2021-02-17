#if NETFRAMEWORK || NETSTANDARD // actually only .NET 3.5 has no SpinWait but this implementation is more efficient

// ReSharper disable once CheckNamespace
namespace System.Threading
{
    internal struct SpinWait
    {
        #region Constants

        private const int yieldThreshold = 10;
        private const int sleep1Threshold = 20;
        private const int maxPower = 16;
        private const int maxSpin = 1 << maxPower;
#if !NET35
        private const int sleep0EveryHowManyYields = 5; 
#endif

        #endregion

        #region Fields

        #region Static Fields

        private static readonly bool isSingleProcessor = Environment.ProcessorCount == 1;

        #endregion

        #region Instance Fields

        private int count;

        #endregion

        #endregion

        #region Methods

        internal void SpinOnce()
        {
            // count & 1 check: interleaving spinning and yield/sleep because Yield/Sleep(0) returns immediately when there are no waiting threads.
            // This also can prevent switching threads between each other if there are more calling Yield/Sleep(0) at the same time
            if (count > yieldThreshold && (count & 1) == 0 || isSingleProcessor)
            {
#if NET35
                Thread.Sleep(count >= sleep1Threshold ? 1 : 0);
#else
                if (count >= sleep1Threshold)
                    Thread.Sleep(1);
                else
                {
                    int yieldsSoFar = count >= yieldThreshold ? (count - yieldThreshold) >> 1 : count;
                    if ((yieldsSoFar % sleep0EveryHowManyYields) == (sleep0EveryHowManyYields - 1))
                        Thread.Sleep(0);
                    else
                        Thread.Yield();
                }
#endif
            }
            else
                Thread.SpinWait(count <= maxPower ? 1 << count : maxSpin);

            count = count == Int32.MaxValue ? sleep1Threshold : count + 1;
        }

        #endregion
    }
}

#endif