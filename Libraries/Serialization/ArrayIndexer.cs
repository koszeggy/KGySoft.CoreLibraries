namespace KGySoft.Serialization
{
    /// <summary>
    /// Provides a general indexer for any-dimension array for any bounds
    /// </summary>
    internal sealed class ArrayIndexer
    {
        readonly int totalLength;
        readonly int lastIndexLength;
        readonly int[] lengths;
        readonly int[] lowerBounds;
        int current;
        readonly int[] currentZeroBased;

        internal ArrayIndexer(int[] lengths, int[] lowerBounds = null)
        {
            lastIndexLength = lengths[lengths.Length - 1];
            totalLength = lengths[0];
            for (int i = 1; i < lengths.Length; i++)
            {
                totalLength *= lengths[i];
            }
            this.lengths = lengths;
            this.lowerBounds = lowerBounds ?? new int[lengths.Length];
            currentZeroBased = new int[lengths.Length];
            current = -1;
        }

        internal bool MoveNext()
        {
            current++;
            if (current != 0)
            {
                int currLastIndex = current % lastIndexLength;
                currentZeroBased[currentZeroBased.Length - 1] = currLastIndex;
                if (currLastIndex == 0)
                {
                    for (int i = currentZeroBased.Length - 2; i >= 0; i--)
                    {
                        currentZeroBased[i]++;
                        if (currentZeroBased[i] != lengths[i])
                            break;
                        currentZeroBased[i] = 0;
                    }
                }
            }
            return current < totalLength;
        }

        internal int[] Current
        {
            get
            {
                int[] result = new int[currentZeroBased.Length];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = currentZeroBased[i] + lowerBounds[i];
                }
                return result;
            }
        }
    }
}
