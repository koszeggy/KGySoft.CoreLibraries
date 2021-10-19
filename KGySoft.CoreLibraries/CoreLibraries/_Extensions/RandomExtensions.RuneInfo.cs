#if NETCOREAPP3_0_OR_GREATER
#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensions.RuneInfo.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

#endregion

namespace KGySoft.CoreLibraries
{
    partial class RandomExtensions
    {
        private static class RuneInfo
        {
            #region RuneSet struct

            [DebuggerDisplay("Length: {Length}; Subsets: {sets.Length}")]
            private readonly struct RuneSet
            {
                #region Fields

                #region Internal Fields

                internal readonly int Length;

                #endregion

                #region Private Fields

                private readonly (int First, int Count)[] sets;

                #endregion

                #endregion

                #region Indexers

                internal Rune this[int index]
                {
                    get
                    {
                        for (int i = 0; i < sets.Length; i++)
                        {
                            if (index < sets[i].Count)
                                return (Rune)(sets[i].First + index);
                            index -= sets[i].Count;
                        }

                        // should not occur for an internal call
                        return Throw.InternalError<Rune>(Res.ArgumentOutOfRange);
                    }
                }

                #endregion

                #region Constructors

                internal RuneSet(IList<(int First, int Count)> sets)
                {
                    this.sets = new (int, int)[sets.Count];
                    Length = 0;
                    for (int i = 0; i < sets.Count; i++)
                    {
                        this.sets[i] = sets[i];
                        Length += sets[i].Count;
                    }
                }

                #endregion

            }

            #endregion

            #region Constants

            private const int lowRangeMaxValue = 0xD7FF;
            private const int highRangeMinValue = 0xE000;
            private const int rangesGapSize = highRangeMinValue - lowRangeMaxValue - 1;

            #endregion

            #region Fields

            #region Internal Fields

            internal static readonly Rune MinValue = new Rune(0);
            internal static readonly Rune MaxValue = new Rune(0x10FFFF);

            #endregion

            #region Private Fields

            private static Dictionary<UnicodeCategory, RuneSet>? runeCacheByCategory;

            #endregion

            #endregion

            #region Methods

            #region Internal Methods

            internal static int GetInclusiveRange(Rune minValue, Rune maxValue)
            {
                int min = minValue.Value;
                int max = maxValue.Value;
                Debug.Assert(max > min);
                return max - min + 1 - (max <= lowRangeMaxValue || min >= highRangeMinValue ? 0 : rangesGapSize);
            }

            internal static Rune GetRuneByIndex(int index) => new Rune(index > lowRangeMaxValue ? index + rangesGapSize : index);

            internal static Rune GetRandomRune(Random random, UnicodeCategory category)
            {
                if (runeCacheByCategory == null)
                    InitCache();
                if (!runeCacheByCategory!.TryGetValue(category, out RuneSet set))
                    Throw.ArgumentOutOfRangeException(nameof(category), Res.ArgumentOutOfRange);
                return set[random.Next(set.Length)];
            }

            #endregion

            #region Private Methods

            private static void InitCache()
            {
                var dict = new Dictionary<UnicodeCategory, List<(int, int)>>(ComparerHelper<UnicodeCategory>.EqualityComparer);

                UnicodeCategory? lastCategory = null;
                int currentStart = 0;
                int currentLength = 0;
                for (int i = 0; i <= MaxValue.Value; i = i == lowRangeMaxValue ? highRangeMinValue : i + 1)
                {
                    UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(i);
                    if (category == lastCategory && i != highRangeMinValue)
                    {
                        currentLength += 1;
                        continue;
                    }

                    if (lastCategory.HasValue)
                    {
                        if (dict.TryGetValue(lastCategory.Value, out var list))
                            list.Add((currentStart, currentLength));
                        else
                            dict.Add(lastCategory.Value, new List<(int, int)> { (currentStart, currentLength) });
                    }

                    lastCategory = category;
                    currentStart = i;
                    currentLength = 1;
                }

                // adding the last set
                if (dict.TryGetValue(lastCategory!.Value, out var set))
                    set.Add((currentStart, currentLength));
                else
                    dict.Add(lastCategory.Value, new List<(int, int)> { (currentStart, currentLength) });

                var result = new Dictionary<UnicodeCategory, RuneSet>(dict.Count, ComparerHelper<UnicodeCategory>.EqualityComparer);
                foreach (KeyValuePair<UnicodeCategory, List<(int, int)>> item in dict)
                    result.Add(item.Key, new RuneSet(item.Value));
                runeCacheByCategory = result;
            }

            #endregion

            #endregion
        }
    }
}
#endif