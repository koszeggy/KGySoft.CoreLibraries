#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: RandomExtensions.WordGenerator.cs
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
using System.Linq;
using System.Security;

#endregion

#region Suppressions

#if NET5_0_OR_GREATER
#pragma warning disable CA2249 // Consider using 'string.Contains' instead of 'string.IndexOf' - there is no String.Contains(char) method in some targeted platforms
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    public static partial class RandomExtensions
    {
        #region WordGenerator class

        private static class WordGenerator
        {
            #region GeneratorContext struct

            private ref struct GeneratorContext
            {
                #region Fields

                #region Internal Fields

                internal readonly Random Random;

                #endregion

                #region Private Fields

                [SecurityCritical]
                private MutableStringBuilder result;

                #endregion

                #endregion

                #region Properties and Indexers

                #region Properties

                internal int CurrentWordStartPosition { get; private set; }
                internal int RemainingWordLength { get; private set; }
                internal int RemainingSentenceLength { get; private set; }

                internal char CurrentWordFirstLetter
                {
                    [SecurityCritical]
                    get => result.Length == 0 ? default : result[CurrentWordStartPosition];
                }

                internal char LastLetter
                {
                    [SecurityCritical]
                    get => result.Length == 0 ? default : result[result.Length - 1];
                }

                internal int CurrentWordLength
                {
                    [SecurityCritical]
                    get => result.Length - CurrentWordStartPosition;
                }

                #endregion

                #region Indexers

                public char this[int index]
                {
                    [SecurityCritical]
                    get => result[index];
                    [SecurityCritical]
                    set => result[index] = value;
                }

                #endregion

                #endregion

                #region Constructors

                [SecurityCritical]
                internal GeneratorContext(Random random, in MutableString target) : this()
                {
                    Random = random;
                    result = new MutableStringBuilder(target);
                }

                #endregion

                #region Methods

                #region Public Methods

                [SecuritySafeCritical] 
                public override string ToString() => result.ToString();

                #endregion

                #region Internal Methods

                [SecurityCritical]
                internal char GetLetterFromEnd(int index) => result[result.Length - index - 1];

                [SecurityCritical]
                internal void StartNewWord(int length)
                {
                    Debug.Assert(length <= result.Capacity - result.Length, "Length too long");
                    CurrentWordStartPosition = result.Length;
                    RemainingWordLength = length;
                }

                internal void StartNewSentence(int length) => RemainingSentenceLength = length;

                [SecurityCritical]
                internal void AddChar(char c)
                {
                    result.Append(c);

                    // if currently out of word or sentence, these can go below zero
                    RemainingWordLength -= 1;
                    RemainingSentenceLength -= 1;
                }

                [SecurityCritical]
                internal void Insert(int index, char c)
                {
                    result.Insert(index, c);

                    RemainingSentenceLength -= 1;
                    if (index < CurrentWordStartPosition)
                        CurrentWordStartPosition += 1;
                    else
                        RemainingWordLength -= 1;
                }

                [SecurityCritical]
                internal void Insert(int index, string s)
                {
                    result.Insert(index, s);

                    RemainingSentenceLength -= s.Length;
                    if (index < CurrentWordStartPosition)
                        CurrentWordStartPosition += s.Length;
                    else
                        RemainingWordLength -= s.Length;
                }

                #endregion

                #endregion
            }

            #endregion

            #region Constants
            
            private const string vowels = "aeiou";
            private const string consonants = "bcdfghjklmnpqrstvwxyz";
            private const string consonantsAlone = "qwx";
            private const string consonantsNotDoubled = "qwxy";

            #endregion
            
            #region Fields

            private static readonly string[] consonantsNotCombined = { "bcdgkpt", "fv", "jy" };

            #endregion

            #region Methods

            #region Internal Methods

            [SecurityCritical]
            internal static void GenerateWord(Random random, in MutableString target)
            {
                if (target.Length == 0)
                    return;

                var context = new GeneratorContext(random, target);
                GenerateWord(ref context, target.Length);
            }

            [SecurityCritical]
            internal static void GenerateSentence(Random random, in MutableString target)
            {
                if (target.Length == 0)
                    return;

                if (target.Length == 1)
                {
                    GenerateWord(random, target);
                    target.ToUpper();
                    return;
                }

                var context = new GeneratorContext(random, target);
                context.StartNewSentence(target.Length);
                GenerateFirstWord(ref context);
                while (context.RemainingSentenceLength > 1)
                    GenerateNextWord(ref context);
                GenerateSentenceEnd(ref context);
                Debug.Assert(context.RemainingSentenceLength == 0);
            }

            #endregion

            #region Private Methods

            [SecurityCritical]
            private static void GenerateWord(ref GeneratorContext context, int length)
            {
                context.StartNewWord(length);
                context.AddChar(GetFirstLetter(ref context));
                while (context.RemainingWordLength > 0)
                    context.AddChar(GetNextLetter(ref context));
            }

            private static char GetFirstLetter(ref GeneratorContext context)
                => context.Random.NextDouble() < (context.RemainingWordLength == 1 ? 0.9 : 0.5)
                ? GetVowel(ref context)
                : GetConsonant(ref context);

            private static char GetVowel(ref GeneratorContext context) => vowels[context.Random.Next(vowels.Length)];

            private static char GetConsonant(ref GeneratorContext context) => consonants[context.Random.Next(consonants.Length)];

            [SecurityCritical]
            private static char GetNextLetter(ref GeneratorContext context)
                => vowels.IndexOf(context.LastLetter) >= 0
                ? GetVowelSuccessor(ref context)
                : GetConsonantSuccessor(ref context);

            [SecurityCritical]
            private static char GetVowelSuccessor(ref GeneratorContext context)
            {
                switch (context.Random.NextDouble())
                {
                    // ReSharper disable once PatternAlwaysMatches - object pattern is less readable
                    // 10% chance for multiple vowels
                    case double d when d < 0.1 && CanAddAnyVowel(ref context):
                        char c;
                        while (!CanAddVowel(ref context, c = GetVowel(ref context))) { }
                        return c;
                    default:
                        return GetConsonant(ref context);
                }
            }

            [SecurityCritical]
            private static char GetConsonantSuccessor(ref GeneratorContext context)
            {
                switch (context.Random.NextDouble())
                {
                    // 25% chance for multiple consonants - 10+% for doubling the last one
                    case double d when d < 0.25 && CanAddAnyConsonant(ref context):
                        char last = context.LastLetter;
                        if (d < 0.1 && CanAddConsonant(ref context, last))
                            return last;
                        else
                        {
                            char c;
                            while (!CanAddConsonant(ref context, c = GetConsonant(ref context))) { }
                            return c;
                        }
                    default:
                        return GetVowel(ref context);
                }
            }

            [SecurityCritical]
            private static bool CanAddAnyVowel(ref GeneratorContext context)
            {
                // last char is consonant
                if (vowels.IndexOf(context.LastLetter) < 0)
                    return true;

                // multiple vowels at first position are enabled if length is at least 3 letters
                if (context.CurrentWordLength == 1)
                    return context.RemainingWordLength > 1;

                // not already 2 vowels at the end
                return vowels.IndexOf(context.GetLetterFromEnd(1)) < 0;
            }

            [SecurityCritical]
            private static bool CanAddVowel(ref GeneratorContext context, char c)
                => c != context.LastLetter; // doubled vowel is not allowed

            [SecurityCritical]
            private static bool CanAddAnyConsonant(ref GeneratorContext context)
            {
                char last = context.LastLetter;

                // last char is vowel
                if (vowels.IndexOf(last) >= 0)
                    return true;

                // consonants, which cannot be combined with other consonant
                if (consonantsAlone.IndexOf(last) >= 0)
                    return false;

                // multiple consonants at first position are enabled if length is at least 3 letters
                if (context.CurrentWordLength == 1)
                    return context.RemainingWordLength > 1;

                // not already 2 consonants at the end
                return vowels.IndexOf(context.GetLetterFromEnd(1)) >= 0;
            }

            [SecurityCritical]
            private static bool CanAddConsonant(ref GeneratorContext context, char c)
            {
                char last = context.LastLetter;

                // after a vowel: always ok
                if (vowels.IndexOf(last) >= 0)
                    return true;

                // consonants that can follow vowel only
                if (consonantsAlone.IndexOf(c) >= 0)
                    return false;

                // doubled consonant is not allowed at first position and come consonants cannot be doubled
                if (c == last)
                    return context.CurrentWordLength > 1 && consonantsNotDoubled.IndexOf(c) < 0;

                // some consonants cannot be combined
                return consonantsNotCombined.All(group => !(group.IndexOf(c) >= 0 && group.IndexOf(last) >= 0));
            }

            [SecurityCritical]
            private static void GenerateFirstWord(ref GeneratorContext context)
            {
                Debug.Assert(context.RemainingSentenceLength > 1);

                // Up to remaining length - 1, max 10. 1 char must be left to close the sentence.
                int wordLength = context.Random.Next(1, Math.Min(context.RemainingSentenceLength, 11));

                // Length - 2 is not good because the will be no more place for another word. 1 or at least 3 must be left.
                if (wordLength == context.RemainingSentenceLength - 2)
                    wordLength += wordLength > 5 ? -1 : 1;

                GenerateWord(ref context, wordLength);
                context[context.CurrentWordStartPosition] = Char.ToUpperInvariant(context.CurrentWordFirstLetter);
                Debug.Assert(context.RemainingSentenceLength >= 3 || context.RemainingSentenceLength == 1);
            }

            [SecurityCritical]
            private static void GenerateNextWord(ref GeneratorContext context)
            {
                Debug.Assert(context.RemainingSentenceLength > 2);

                context.AddChar(' ');

                // Up to remaining length - 1, max 10. 1 chars must be left to close the sentence.
                int wordLength = context.Random.Next(1, Math.Min(context.RemainingSentenceLength, 11));
                GenerateWord(ref context, wordLength);

                // punctuation
                if (context.RemainingSentenceLength == 2 || (context.RemainingSentenceLength > 4 && context.Random.NextDouble() < 0.1d))
                    AddPunctuation(ref context);
                // variation
                else if (context.Random.NextDouble() < 0.1d)
                    AddVariation(ref context);

                Debug.Assert(context.RemainingSentenceLength >= 3 || context.RemainingSentenceLength == 1);
            }

            [SecurityCritical]
            private static void AddPunctuation(ref GeneratorContext context)
            {
                Debug.Assert(context.RemainingSentenceLength == 2 || context.RemainingSentenceLength > 4);
                Debug.Assert(context[context.CurrentWordStartPosition - 1] == ' ');
                switch (context.Random.NextDouble())
                {
                    case < 0.1d:
                        context.Insert(context.CurrentWordStartPosition - 1, ';');
                        return;

                    case < 0.2d:
                        context.Insert(context.CurrentWordStartPosition - 1, ':');
                        return;

                    case double d when d < 0.3d && context.RemainingSentenceLength > 2:
                        context.Insert(context.CurrentWordStartPosition - 1, " -");
                        return;

                    case double d when d < 0.35d && context.RemainingSentenceLength > 2:
                        context.Insert(context.CurrentWordStartPosition, '(');
                        context.AddChar(')');
                        return;

                    case double d when d < 0.4d && context.RemainingSentenceLength > 2:
                        context.Insert(context.CurrentWordStartPosition, '\'');
                        context.AddChar('\'');
                        return;

                    case double d when d < 0.45d && context.RemainingSentenceLength > 2:
                        context.Insert(context.CurrentWordStartPosition, '"');
                        context.AddChar('"');
                        return;

                    default:
                        context.Insert(context.CurrentWordStartPosition - 1, ',');
                        return;
                }
            }

            [SecurityCritical]
            private static void AddVariation(ref GeneratorContext context)
            {
                Debug.Assert(context[context.CurrentWordStartPosition - 1] == ' ');
                switch (context.Random.NextDouble())
                {
                    case double d when d < 0.05d:
                        context[context.CurrentWordStartPosition - 1] = '/';
                        return;
                    case double d when d < 0.1d:
                        for (int i = 0; i < context.CurrentWordLength; i++)
                        {
                            int pos = context.CurrentWordStartPosition + i;
                            context[pos] = Char.ToUpperInvariant(context[pos]);
                        }
                        return;
                    case double d when d < 0.2d:
                        context[context.CurrentWordStartPosition - 1] = '-';
                        return;
                    case double d when d < 0.3d && context.CurrentWordLength > 3:
                        context[context.Random.Next(context.CurrentWordStartPosition + 1, context.CurrentWordStartPosition + context.CurrentWordLength - 1)] = '\'';
                        return;
                    default:
                        context[context.CurrentWordStartPosition] = Char.ToUpperInvariant(context.CurrentWordFirstLetter);
                        return;
                }
            }

            [SecurityCritical]
            private static void GenerateSentenceEnd(ref GeneratorContext context)
            {
                switch (context.Random.NextDouble())
                {
                    case double d when d < 0.02d:
                        context.AddChar('?');
                        return;
                    case double d when d < 0.1d:
                        context.AddChar('!');
                        return;
                    default:
                        context.AddChar('.');
                        return;
                }
            }

            #endregion

            #endregion
        }

        #endregion
    }
}
