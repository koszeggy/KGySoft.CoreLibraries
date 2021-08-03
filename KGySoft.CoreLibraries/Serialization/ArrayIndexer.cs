#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ArrayIndexer.cs
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

        private int pos;

        #endregion

        #region Properties

        internal int[] Current { get; }

        #endregion

        #region Constructors

        internal ArrayIndexer(Array array)
        {
            if (array == null!)
                Throw.ArgumentNullException(Argument.array);

            totalLength = array.Length;
            int rank = array.Rank;
            lengths = new int[rank];
            lowerBounds = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                lengths[i] = array.GetLength(i);
                lowerBounds[i] = array.GetLowerBound(i);
            }

            lastIndexLength = lengths[rank - 1];
            currentZeroBased = new int[rank];
            Current = new int[rank];
            pos = -1;
        }

        internal ArrayIndexer(int[] lengths, int[]? lowerBounds = null)
        {
            lastIndexLength = lengths[lengths.Length - 1];
            totalLength = lengths[0];
            for (int i = 1; i < lengths.Length; i++)
                totalLength *= lengths[i];
            this.lengths = lengths;
            this.lowerBounds = lowerBounds ?? new int[lengths.Length];
            currentZeroBased = new int[lengths.Length];
            Current = new int[lengths.Length];
            pos = -1;
        }

        #endregion

        #region Methods

        internal bool MoveNext()
        {
            if (++pos == totalLength)
                return false;

            if (pos > 0)
            {
                int currLastIndex = pos % lastIndexLength;
                currentZeroBased[currentZeroBased.Length - 1] = currLastIndex;
                if (currLastIndex == 0)
                {
                    for (int i = currentZeroBased.Length - 2; i >= 0; i--)
                    {
                        currentZeroBased[i] += 1;
                        if (currentZeroBased[i] != lengths[i])
                            break;
                        currentZeroBased[i] = 0;
                    }
                }
            }

            for (int i = 0; i < Current.Length; i++)
                Current[i] = currentZeroBased[i] + lowerBounds[i];
            return true;
        }

        #endregion
    }
}
