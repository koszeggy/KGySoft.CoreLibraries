#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArrayIndexer.cs
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

namespace KGySoft.Serialization
{
    /// <summary>
    /// Provides a general indexer for any-dimension array for any bounds
    /// </summary>
    internal sealed class ArrayIndexer
    {
        #region Fields

        private readonly int totalLength;
        private readonly int lastIndexLength;
        private readonly int[] lengths;
        private readonly int[] lowerBounds;
        private readonly int[] currentZeroBased;

        private int current;

        #endregion

        #region Properties

        internal int[] Current
        {
            get
            {
                int[] result = new int[currentZeroBased.Length];
                for (int i = 0; i < result.Length; i++)
                    result[i] = currentZeroBased[i] + lowerBounds[i];
                return result;
            }
        }

        #endregion

        #region Constructors

        internal ArrayIndexer(int[] lengths, int[] lowerBounds = null)
        {
            lastIndexLength = lengths[lengths.Length - 1];
            totalLength = lengths[0];
            for (int i = 1; i < lengths.Length; i++)
                totalLength *= lengths[i];
            this.lengths = lengths;
            this.lowerBounds = lowerBounds ?? new int[lengths.Length];
            currentZeroBased = new int[lengths.Length];
            current = -1;
        }

        #endregion

        #region Methods

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

        #endregion
    }
}
