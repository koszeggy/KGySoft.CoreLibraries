using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace KGySoft.Libraries
{
    public static partial class RandomExtensions
    {
        private static class WordGenerator
        {
            private struct GeneratorContext
            {
                private readonly StringBuilder result;
                internal readonly Random Random;

                internal int CurrentWordStartPosition { get; private set; }
                internal int RemainingWordLength { get; private set; }
                internal int RemainingSentenceLength { get; private set; }

                internal char CurrentWordFirstLetter => result.Length == 0 ? default : result[CurrentWordStartPosition];
                internal char LastLetter => result.Length == 0 ? default : result[result.Length - 1];
                internal char GetLetterFromEnd(int index) => result[result.Length - index - 1];
                internal int CurrentWordLength => result.Length - CurrentWordStartPosition;

                internal GeneratorContext(Random random) : this()
                {
                    Random = random;
                    result = new StringBuilder();
                }

                public char this[int index]
                {
                    get => result[index];
                    set => result[index] = value;
                }

                internal void StartNewWord(int length)
                {
                    CurrentWordStartPosition = result.Length;
                    RemainingWordLength = length;
                }

                internal void StartNewSentence(int length) => RemainingSentenceLength = length;

                internal void AddChar(char c)
                {
                    result.Append(c);

                    // if currently out of word or sentence, these can go below zero
                    RemainingWordLength--;
                    RemainingSentenceLength--;
                }

                public override string ToString() => result.ToString();

                public void Insert(int index, char c)
                {
                    result.Insert(index, c);

                    RemainingSentenceLength--;
                    if (index < CurrentWordStartPosition)
                        CurrentWordStartPosition++;
                    else
                        RemainingWordLength--;
                }

                public void Insert(int index, string s)
                {
                    result.Insert(index, s);

                    RemainingSentenceLength -= s.Length;
                    if (index < CurrentWordStartPosition)
                        CurrentWordStartPosition += s.Length;
                    else
                        RemainingWordLength -= s.Length;
                }
            }

            private static string vowels = "aeiou";
            private static string consonants = "bcdfghjklmnpqrstvwxyz";
            private static string consonantsAlone = "qwx";
            private static string consonantsNotDoubled = "qwxy";
            private static readonly string[] consonantsNotCombined = { "bcdgkpt", "fv", "jy" };

            internal static string GenerateWord(Random random, int length)
            {
                var context = new GeneratorContext(random);
                GenerateWord(ref context, length);
                return context.ToString();
            }

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
            private static char GetNextLetter(ref GeneratorContext context)
                => vowels.IndexOf(context.LastLetter) >= 0
                    ? GetVowelSuccessor(ref context) 
                    : GetConsonantSuccessor(ref context);

            private static char GetVowelSuccessor(ref GeneratorContext context)
            {
                switch (context.Random.NextDouble())
                {
                    // 10% chance for multiple vowels
                    case double d when d < 0.1 && CanAddAnyVowel(ref context):
                        char c;
                        while (!CanAddVowel(ref context, c = GetVowel(ref context))) { }
                        return c;
                    default:
                        return GetConsonant(ref context);
                }
            }

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

            private static bool CanAddVowel(ref GeneratorContext context, char c) => c != context.LastLetter; // doubled vowel is not allowed

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

            private static bool CanAddConsonant(ref GeneratorContext context, char c)
            {
                char last = context.LastLetter;

                // after a vowel: alyaws ok
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

            public static string GenerateSentence(Random random, int length)
            {
                if (length == 0)
                    return String.Empty;

                if (length == 1)
                    return GenerateWord(random, 1).ToUpperInvariant();

                var context = new GeneratorContext(random);
                context.StartNewSentence(length);
                GenerateFirstWord(ref context);
                while (context.RemainingSentenceLength > 1)
                    GenerateNextWord(ref context);
                GenerateSentenceEnd(ref context);
                Debug.Assert(context.RemainingSentenceLength == 0);
                return context.ToString();
            }

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

            private static void GenerateNextWord(ref GeneratorContext context)
            {
                Debug.Assert(context.RemainingSentenceLength > 2);

                context.AddChar(' ');

                // Up to remaining length - 2, max 10. 2 chars must be left to the space and to close the sentence.
                int wordLength = context.Random.Next(1, Math.Min(context.RemainingSentenceLength - 1, 11));
                GenerateWord(ref context, wordLength);

                // punctuation
                if (context.RemainingSentenceLength == 2 || (context.RemainingSentenceLength > 4 && context.Random.NextDouble() < 0.1d))
                    AddPunctuation(ref context);
                // variation
                else if (context.Random.NextDouble() < 0.1d)
                    AddVariation(ref context);

                Debug.Assert(context.RemainingSentenceLength >= 3 || context.RemainingSentenceLength == 1);
            }

            private static void AddPunctuation(ref GeneratorContext context)
            {
                Debug.Assert(context.RemainingSentenceLength == 2 || context.RemainingSentenceLength > 4);
                Debug.Assert(context[context.CurrentWordStartPosition - 1] == ' ');
                switch (context.Random.NextDouble())
                {
                    case double d when d < 0.1d:
                        context.Insert(context.CurrentWordStartPosition - 1, ';');
                        return;

                    case double d when d < 0.2d:
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
        }

    }
}
