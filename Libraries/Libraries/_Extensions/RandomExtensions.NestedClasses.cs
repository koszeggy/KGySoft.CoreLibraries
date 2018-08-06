using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
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

        private static class ObjectGenerator
        {
            private static readonly Dictionary<Type, Func<Random, GenerateObjectSettings, object>> knownTypes =
                new Dictionary<Type, Func<Random, GenerateObjectSettings, object>>
                {
                    // primitive types
                    { typeof(bool), (rnd, settings) => rnd.NextBoolean() },
                    { typeof(byte), (rnd, settings) => rnd.NextByte() },
                    { typeof(sbyte), (rnd, settings) => rnd.NextSByte() },
                    { typeof(char), (rnd, settings) => rnd.NextChar() },
                    { typeof(short), (rnd, settings) => rnd.NextUInt16() },
                    { typeof(ushort), (rnd, settings) => rnd.NextInt16() },
                    { typeof(int), (rnd, settings) => rnd.NextInt32() },
                    { typeof(uint), (rnd, settings) => rnd.NextUInt32() },
                    { typeof(long), (rnd, settings) => rnd.NextInt64() },
                    { typeof(ulong), (rnd, settings) => rnd.NextUInt64() },

                    // floating points
                    { typeof(float), (rnd, settings) => rnd.NextSingle(Single.MinValue, Single.MaxValue) },
                    { typeof(double), (rnd, settings) => rnd.NextDouble(Double.MinValue, Double.MaxValue) },
                    { typeof(decimal), (rnd, settings) => rnd.NextDecimal(Decimal.MinValue, Decimal.MaxValue) },

                    // strings
                    { typeof(string), (rnd, settings) => GenerateString(rnd, null, settings) },
                    { typeof(StringBuilder), (rnd, settings) => GenerateStringBuilder(rnd, null, settings) },
                    { typeof(Uri), GenerateUri },

                    // guid
                    { typeof(Guid), (rnd, settings) => rnd.NextGuid() },

                    // date and time
                    { typeof(DateTime), (rnd, settings) => rnd.NextDateTime() },
                    { typeof(DateTimeOffset), (rnd, settings) => rnd.NextDateTimeOffset() },
                    { typeof(TimeSpan), (rnd, settings) => rnd.NextTimeSpan() },
                };

            internal static object GenerateObject(Random random, Type type, GenerateObjectSettings settings)
            {
                // 0.) null
                if (settings.ChanceOfNull > 0 && random.NextDouble() > settings.ChanceOfNull)
                    return null;

                if (type.IsNullable())
                    type = Nullable.GetUnderlyingType(type);

                // ReSharper disable once AssignNullToNotNullAttribute
                // 1.) known type
                if (knownTypes.TryGetValue(type, out var knownGenerator))
                    return knownGenerator.Invoke(random, settings);

                // 2.) enum
                // ReSharper disable once PossibleNullReferenceException
                if (type.IsEnum)
                    return GenerateEnum(random, type);

                // 3.) array
                if (type.IsArray)
                    return GenerateArray(type.GetElementType(), settings);

                // 4.) supported collection
                ConstructorInfo ci;
                Type elementType;
                throw new NotImplementedException();
                //if (IsSupportedCollection(type, out ci, out elementType))
                //{
                //    if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                //    {
                //        var args = elementType.GetGenericArguments();
                //        IDictionary dict = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(args[0], args[1]));
                //        for (int i = 0; i < collectionsLength; i++)
                //        {
                //            var key = GenerateObject(args[0], collectionsLength);
                //            if (key == null)
                //            {
                //                break;
                //            }

                //            var value = GenerateObject(args[1], collectionsLength);
                //            dict[key] = value;
                //        }

                //        return type == dict.GetType() ? dict : ci.Invoke(new[] { dict });
                //    }

                //    var array = GenerateArray(elementType, collectionsLength);
                //    return ci.Invoke(new[] { array });
                //}

                //// 5.) key-value pair
                //if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                //{
                //    var args = type.GetGenericArguments();
                //    var key = GenerateObject(args[0], collectionsLength);
                //    var value = GenerateObject(args[1], collectionsLength);
                //    return Activator.CreateInstance(type, new[] { key, value });
                //}

                //// 6.) abstract type or interface: null
                //if (type.IsAbstract || type.IsInterface)
                //{
                //    return null;
                //}

                //// 7.) struct: returning a default instance
                //if (type.IsValueType)
                //{
                //    return Activator.CreateInstance(type);
                //}

                //// 8.) any other object: create it with or without default constructor
                //var result = type.GetConstructor(Type.EmptyTypes) == null ? FormatterServices.GetUninitializedObject(type) : Activator.CreateInstance(type);
                //FillProperties(result, collectionsLength);
                //return result;
            }

            private static string GenerateString(Random random, string memberName, GenerateObjectSettings settings)
            {
                // ... else
                (int minLength, int maxLength) = settings.StringCreationOptions == RandomString.Sentence ? settings.SentencesLength : settings.StringsLength;
                return random.NextString(minLength, maxLength, settings.StringCreationOptions.GetValueOrDefault(RandomString.Ascii));
            }

            private static Array GenerateArray(Type getElementType, GenerateObjectSettings settings)
            {
                throw new NotImplementedException();
            }

            private static StringBuilder GenerateStringBuilder(Random random, string memberName, GenerateObjectSettings settings) 
                => new StringBuilder(GenerateString(random, memberName, settings));

            private static Uri GenerateUri(Random random, GenerateObjectSettings settings) 
                => new Uri($"http://{random.NextString(strategy: RandomString.LowerCaseWord)}.{random.NextString(3, 3, RandomString.LowerCaseLetters)}");

            private static object GenerateEnum(Random random, Type type)
            {
                var values = Enum.GetValues(type);
                return values.GetValue(random.Next(values.Length));
            }
        }
    }
}
