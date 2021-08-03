#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensions.CharSet.cs
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

using System.Linq;

#endregion

namespace KGySoft.CoreLibraries
{
    partial class RandomExtensions
    {
        private readonly struct CharSet
        {
            #region Fields

            #region Static Fields

            #region Private Fields

            private static readonly (char, short) digits = ('0', 10);
            private static readonly (char, short) nonZeroDigits = ('1', 9);
            private static readonly (char, short) lowerCaseLetters = ('a', 26);
            private static readonly (char, short) upperCaseLetters = ('A', 26);
            private static readonly (char, short) ascii = (' ', 95);

            #endregion

            #region Internal Fields
            
            // Note: These must come after the private fields because they use them
            internal static readonly CharSet Digits = new CharSet(digits);
            internal static readonly CharSet NonZeroDigits = new CharSet(nonZeroDigits);
            internal static readonly CharSet LowerCaseLetters = new CharSet(lowerCaseLetters);
            internal static readonly CharSet UpperCaseLetters = new CharSet(upperCaseLetters);
            internal static readonly CharSet Letters = new CharSet(lowerCaseLetters, upperCaseLetters);
            internal static readonly CharSet LettersAndDigits = new CharSet(digits, lowerCaseLetters, upperCaseLetters);
            internal static readonly CharSet Ascii = new CharSet(ascii);

            #endregion

            #endregion

            #region Instance Fields

            #region Internal Fields

            internal readonly int Length;

            #endregion

            #region Private Fields

            private readonly (char First, short Count)[] sets; 
            
            #endregion

            #endregion

            #endregion

            #region Indexers

            internal char this[int index]
            {
                get
                {
                    for (int i = 0; i < sets.Length; i++)
                    {
                        if (index < sets[i].Count)
                            return (char)(sets[i].First + index);
                        index -= sets[i].Count;
                    }

                    // should not occur for an internal call
                    return Throw.InternalError<char>(Res.ArgumentOutOfRange);
                }
            }

            #endregion

            #region Constructors

            private CharSet(params (char First, short Count)[] sets)
            {
                this.sets = sets;
                Length = 0;
                for (int i = 0; i < sets.Length; i++)
                    Length += sets[i].Count;
            }

            #endregion

            #region Methods

            public override string ToString() => sets.Select(set => $"[{set.First}..{(char)(set.First + set.Count - 1)}]").Join("; ");

            #endregion
        }
    }
}
