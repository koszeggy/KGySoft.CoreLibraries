using System;
using System.Text;

namespace KGySoft.Libraries
{
    internal static class WordGenerator
    {
        private static string vowels = "aeiou";
        private static string consonants = "bcdfghjklmnpqrstvwxyz";
        private static string consonantAlone = "qwx";
        private static string consonantNoTogether = "bcdgkpt";

        internal static string GenerateWord(Random random, int length)
        {
            var result = new StringBuilder(length);
            result.Append(GetFirstLetter(random));

            for (int i = 1; i < length; i++)
                result.Append(GetNextLetter(random, result));

            return result.ToString();
        }

        private static char GetFirstLetter(Random random) => random.NextDouble() < 0.5 ? GetVowel(random) : GetConsonant(random);
        private static char GetVowel(Random random) => vowels[random.Next(vowels.Length)];
        private static char GetConsonant(Random random) => consonants[random.Next(consonants.Length)];
        private static char GetNextLetter(Random random, StringBuilder sb) => vowels.IndexOf(sb[sb.Length - 1]) >= 0 ? GetVowelSuccessor(random, sb) : GetConsonantSuccessor(random, sb);

        private static char GetVowelSuccessor(Random random, StringBuilder sb)
        {
            switch (random.NextDouble())
            {
                // 10% chance for multiple vowels
                case double d when d < 0.1 && CanAddVowel(sb):
                    char c;
                    while (!CanAddVowel(sb, c = GetVowel(random))) ;
                    return c;
                default:
                    return GetConsonant(random);
            }
        }

        private static char GetConsonantSuccessor(Random random, StringBuilder sb)
        {
            switch (random.NextDouble())
            {
                // 25% chance for multiple consonants - 10+% for doubling the last one
                case double d when d < 0.25 && CanAddConsonant(sb):
                    char last = sb[sb.Length - 1];
                    if (d < 0.1 && CanAddConsonant(sb, last))
                        return last;
                    else
                    {
                        char c;
                        while (!CanAddConsonant(sb, c = GetConsonant(random))) ;
                        return c;
                    }
                default:
                    return GetVowel(random);
            }
        }

        private static bool CanAddVowel(StringBuilder sb)
        {
            // last char is consonant
            if (vowels.IndexOf(sb[sb.Length - 1]) < 0)
                return true;

            // multiple vowels at first position are enabled if length is at least 3 letters
            if (sb.Length == 1)
                return sb.Capacity > 2;

            // not already 2 vowels at the end
            return vowels.IndexOf(sb[sb.Length - 2]) < 0;
        }

        private static bool CanAddVowel(StringBuilder sb, char c) => c != sb[sb.Length - 1]; // doubled vowel is not allowed

        private static bool CanAddConsonant(StringBuilder sb)
        {
            char last = sb[sb.Length - 1];

            // last char is vowel
            if (vowels.IndexOf(last) >= 0)
                return true;

            // q, w and x can be followed vowel only
            if ("qwx".IndexOf(last) >= 0)
                return false;

            // multiple consonants at first position are enabled if length is at least 3 letters
            if (sb.Length == 1)
                return sb.Capacity > 2;

            // not already 2 consonants at the end
            return vowels.IndexOf(sb[sb.Length - 2]) >= 0;
        }

        private static bool CanAddConsonant(StringBuilder sb, char c)
        {
            char last = sb[sb.Length - 1];

            // q, w and x can follow vowel only
            if ("qwx".IndexOf(c) >= 0)
                return vowels.IndexOf(last) >= 0;

            // doubled consonant is not allowed at first position and y cannot be doubled
            if (c == last)
                return sb.Length > 1 && c != 'y';

            return true;
        }

        public static string GenerateSentence(Random random, int length)
        {
            throw new NotImplementedException();
        }
    }
}
