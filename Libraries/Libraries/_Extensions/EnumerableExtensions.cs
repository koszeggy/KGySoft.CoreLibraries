#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumerableExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2018 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KGySoft.Libraries.Annotations;
using KGySoft.Libraries.Collections;
using KGySoft.Libraries.Reflection;
using KGySoft.Libraries.Resources;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="IEnumerable{T}"/> type.
    /// </summary>
    public static class EnumerableExtensions
    {
        #region Methods

        #region Public Methods

        /// <summary>
        /// Similarly to <see cref="List{T}.ForEach">List{T}.ForEach</see> processes an action on each element of an enumerable collection.
        /// </summary>
        /// <returns>Returns the original list making possible to link it into a LINQ chain.</returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));
            if (action == null)
                throw new ArgumentNullException(nameof(action), Res.Get(Res.ArgumentNull));

            foreach (T item in source)
            {
                action(item);
            }

            return source;
        }

        /// <summary>
        /// Searches for an element in the <paramref name="source"/> enumeration where the specified <paramref name="predicate"/> returns <see langword="true"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the enumeration..</typeparam>
        /// <param name="source">The source enumeration to search.</param>
        /// <param name="predicate">The predicate to use for the search.</param>
        /// <returns>The index of the found element, or -1 if there was no match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
        public static int IndexOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate), Res.Get(Res.ArgumentNull));

            if (source is IList<TSource> list)
            {
                int length = list.Count;
                for (int i = 0; i < length; i++)
                {
                    if (predicate.Invoke(list[i]))
                        return i;
                }

                return -1;
            }

            var index = 0;
            foreach (var item in source)
            {
                if (predicate.Invoke(item))
                    return index;

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Searches for an element in the <paramref name="source"/> enumeration.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements in the enumeration..</typeparam>
        /// <param name="source">The source enumeration to search.</param>
        /// <param name="element">The element to search.</param>
        /// <returns>The index of the found element, or -1 if <paramref name="element"/> was not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static int IndexOf<TSource>(this IEnumerable<TSource> source, TSource element)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));

            var comparer = element is Enum ? (IEqualityComparer<TSource>)EnumComparer<TSource>.Comparer : EqualityComparer<TSource>.Default;
            if (source is IList<TSource> list)
            {
                int length = list.Count;
                for (int i = 0; i < length; i++)
                {
                    if (comparer.Equals(list[i], element))
                        return i;
                }

                return -1;
            }

            var index = 0;
            foreach (TSource item in source)
            {
                if (comparer.Equals(item, element))
                    return index;

                index++;
            }

            return -1;
        }

        /// <summary>
        /// Creates a <see cref="CircularList{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to create a <see cref="CircularList{T}"/> from.</param>
        /// <returns>A <see cref="CircularList{T}"/> that contains elements from the input sequence.</returns>
        /// <remarks>
        /// The method forces immediate query evaluation and returns a <see cref="CircularList{T}"/> that contains the query results.
        /// You can append this method to your query in order to obtain a cached copy of the query results.
        /// </remarks>
        public static CircularList<TSource> ToCircularList<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));

            return new CircularList<TSource>(source);
        }

        /// <summary>
        /// Shuffles an enumerable <paramref name="source"/> (randomizes its elements) using the provided <paramref name="seed"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to shuffle its elements.</param>
        /// <param name="seed">The seed to use for the shuffling.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> which contains the elements of the <paramref name="source"/> in randomized order.</returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, int seed) 
            => Shuffle(source, new Random(seed));

        /// <summary>
        /// Shuffles an enumerable <paramref name="source"/> (randomizes its elements) using a new <see cref="Random"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to shuffle its elements.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> which contains the elements of the <paramref name="source"/> in randomized order.</returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
            => Shuffle(source, new Random());

        /// <summary>
        /// Shuffles an enumerable <paramref name="source"/> (randomizes its elements) using a specified <see cref="Random"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to shuffle its elements.</param>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> which contains the elements of the <paramref name="source"/> in randomized order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random random)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));

            return source.Select(item => new { Order = random.Next(), Value = item }).OrderBy(i => i.Order).Select(i => i.Value);
        }

        /// <summary>
        /// Gets a random element from the enumerable <paramref name="source"/> using a new <see cref="Random"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to select an element from.</param>
        /// <param name="defaultIfEmpty">If <see langword="true"/> and <paramref name="source"/> is empty, the default value of <typeparamref name="T"/> is returned.
        /// If <see langword="false"/>, and <paramref name="source"/> is empty, an <see cref="ArgumentException"/> will be thrown. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A random element from <paramref name="source"/>.</returns>
        public static T GetRandomElement<T>(this IEnumerable<T> source, bool defaultIfEmpty = false)
            => GetRandomElement(source, new Random(), defaultIfEmpty);

        /// <summary>
        /// Gets a random element from the enumerable <paramref name="source"/> using a specified <see cref="Random"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="random">The <see cref="Random"/> instance to use.</param>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to select an element from.</param>
        /// <param name="defaultIfEmpty">If <see langword="true"/> and <paramref name="source"/> is empty, the default value of <typeparamref name="T"/> is returned.
        /// If <see langword="false"/>, and <paramref name="source"/> is empty, an <see cref="ArgumentException"/> will be thrown. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A random element from the <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains no elements and <paramref name="defaultIfEmpty"/> is <see langword="false"/>.</exception>
        public static T GetRandomElement<T>(this IEnumerable<T> source, Random random, bool defaultIfEmpty = false)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.Get(Res.ArgumentNull));
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));

            if (source is IList<T> list)
                return list.Count > 0
                    ? list[random.Next(list.Count)]
                    : defaultIfEmpty ? default(T) : throw new ArgumentException(Res.Get(Res.CollectionEmpty), nameof(source));

#if NET45
            if (source is IReadOnlyList<T> readonlyList)
                return readonlyList.Count > 0
                    ? readonlyList[random.Next(readonlyList.Count)]
                    : defaultIfEmpty ? default(T) : throw new ArgumentException(Res.Get(Res.CollectionEmpty), nameof(source));
#elif !(NET35 || NET40)
#error .NET version is not set or not supported!
#endif

            using (IEnumerator<T> shuffledEnumerator = Shuffle(source, random).GetEnumerator())
            {
                if (!shuffledEnumerator.MoveNext())
                    return defaultIfEmpty ? default(T) : throw new ArgumentException(Res.Get(Res.CollectionEmpty), nameof(source));
                return shuffledEnumerator.Current;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Adds an element to an enumerable collection if possible.
        /// That is, if <paramref name="source"/> implements either the non-generic <see cref="IList"/> or <see cref="IDictionary"/> interfaces,
        /// or the generic <see cref="ICollection{T}"/> interface.
        /// </summary>
        internal static void Add([NoEnumeration]this IEnumerable source, object item)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));

            // ReSharper disable once UsePatternMatching - must be "as" cast due to .NET 3.5 part
            IList list = source as IList;
            if (list != null
#if NET35
                // IList with null element: skip because generic collections below .NET 4 may not support null elements of nullable types
                && item != null
#elif !(NET40 || NET45)
#error .NET version is not set or not supported!
#endif
                )
            {
                list.Add(item);
                return;
            }

            if (item is DictionaryEntry entry && source is IDictionary dictionary)
            {
                dictionary.Add(entry.Key, entry.Value);
                return;
            }

            Type sourceType = source.GetType();
            foreach (Type i in sourceType.GetInterfaces())
            {
                if (i.IsGenericTypeOf(typeof(ICollection<>)))
                {
                    MethodInfo mi = i.GetMethod("Add");
                    if (mi.GetParameters()[0].ParameterType.CanAcceptValue(item))
                    {
                        MethodInvoker.GetMethodInvoker(mi).Invoke(source, item);
                        return;
                    }
                }
            }

#if NET35
            if (list != null) // && item == null
            {
                list.Add(item);
                return;
            }
#elif !(NET40 || NET45)
#error .NET version is not set or not supported!
#endif
            throw new NotSupportedException(Res.Get(Res.EnumerableCannotAdd, item ?? "null", source.GetType()));
        }

        /// <summary>
        /// Clears an enumerable collection if possible.
        /// That is, if <paramref name="source"/> implements either the non-generic <see cref="IList"/> or <see cref="IDictionary"/> interfaces,
        /// or the generic <see cref="ICollection{T}"/> interface.
        /// </summary>
        internal static void Clear([NoEnumeration]this IEnumerable source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.Get(Res.ArgumentNull));

            IList list = source as IList;
            if (list != null)
            {
                list.Clear();
                return;
            }

            IDictionary dictionary = source as IDictionary;
            if (dictionary != null)
            {
                dictionary.Clear();
                return;
            }

            Type sourceType = source.GetType();
            foreach (Type i in sourceType.GetInterfaces())
            {
                if (i.IsGenericTypeOf(typeof(ICollection<>)))
                {
                    MethodInfo mi = i.GetMethod("Clear");
                    MethodInvoker.GetMethodInvoker(mi).Invoke(source);
                    return;
                }
            }

            throw new InvalidOperationException(Res.Get(Res.EnumerableCannotClear, source.GetType().FullName));
        }

        #endregion

        #endregion
    }
}
