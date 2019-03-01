#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumerableExtensions.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2018 - All Rights Reserved
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
using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.Reflection;
#if !NET35
using System.Collections.Concurrent;
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="IEnumerable{T}"/> type.
    /// </summary>
    public static class EnumerableExtensions
    {
        #region Methods

        #region Public Methods

        #region Manipulation

        /// <summary>
        /// Similarly to the <see cref="List{T}.ForEach"><![CDATA[List<T>.ForEach]]></see> method, processes an action on each element of an enumerable collection.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the enumeration.</typeparam>
        /// <param name="source">The source enumeration.</param>
        /// <param name="action">The action to perform on each element.</param>
        /// <returns>Returns the original list making possible to link it into a LINQ chain.</returns>
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.ArgumentNull);
            if (action == null)
                throw new ArgumentNullException(nameof(action), Res.ArgumentNull);

            foreach (T item in source)
            {
                action(item);
            }

            return source;
        }

        /// <summary>
        /// Tries to add the specified <paramref name="item"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to add the <paramref name="item"/> to.</param>
        /// <param name="item">The item to add.</param>
        /// <param name="checkReadOnly"><see langword="true"/> to return <see langword="false"/> if the collection is read-only; <see langword="false"/> to attempt adding the element without checking the read-only state.</param>
        /// <param name="throwError"><see langword="true"/> to forward any exception thrown by an adding method; <see langword="false"/> to suppress inner exceptions and return <see langword="false"/> on failure.</param>
        /// <returns><see langword="true"/> if an adding method could be successfully called; <see langword="false"/> if such method was not found, or <paramref name="checkReadOnly"/> is <see langword="true"/> and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/> and the adding method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="item"/> can be added to the <paramref name="collection"/> if the collection is either an <see cref="ICollection{T}"/>, <see cref="IProducerConsumerCollection{T}"/> or <see cref="IList"/> implementation.</para>
        /// </remarks>
        public static bool TryAdd<T>([NoEnumeration]this IEnumerable<T> collection, T item, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            try
            {
                switch (collection)
                {
                    case ICollection<T> genericCollection:
                        if (checkReadOnly && genericCollection.IsReadOnly)
                            return false;
                        genericCollection.Add(item);
                        return true;
#if !NET35
                    case IProducerConsumerCollection<T> producerConsumerCollection:
                        return producerConsumerCollection.TryAdd(item);
#endif

                    case IList list:
                        if (checkReadOnly && list.IsReadOnly)
                            return false;
                        list.Add(item);
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                if (throwError)
                    throw;
                return false;
            }
        }

        /// <summary>
        /// Tries to add the specified <paramref name="item"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to add the <paramref name="item"/> to.</param>
        /// <param name="item">The item to add.</param>
        /// <param name="checkReadOnly"><see langword="true"/> to return <see langword="false"/> if the collection is read-only; <see langword="false"/> to attempt adding the element without checking the read-only state.</param>
        /// <param name="throwError"><see langword="true"/> to forward any exception thrown by an adding method; <see langword="false"/> to suppress inner exceptions and return <see langword="false"/> on failure.</param>
        /// <returns><see langword="true"/> if an adding method could be successfully called; <see langword="false"/> if such method was not found, or <paramref name="checkReadOnly"/> is <see langword="true"/> and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/> and the adding method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="item"/> can be added to the <paramref name="collection"/> if the collection is either an <see cref="IList"/>, <see cref="IDictionary"/> (if <paramref name="item"/> is a <see cref="DictionaryEntry"/>),
        /// or a generic <see cref="ICollection{T}"/> or <see cref="IProducerConsumerCollection{T}"/> implementation.</para>
        /// </remarks>
        public static bool TryAdd([NoEnumeration]this IEnumerable collection, object item, bool checkReadOnly = true, bool throwError = true)
        {
            IList list = collection as IList;

            try
            {
                // 1.) IList
                // ReSharper disable once UsePatternMatching - must be "as" cast due to the second .NET 3.5 part at the end
                if (list != null)
                {
                    if (checkReadOnly && list.IsReadOnly)
                        return false;
#if NET35
                    // IList with null element: defer because generic collections in .NET 3.5 don't really support null elements of nullable types
                    if (item != null)
#endif
                    {
                        list.Add(item);
                        return true;
                    }
                }

                // 2.) IDictionary
                if (item is DictionaryEntry entry && collection is IDictionary dictionary)
                {
                    if (checkReadOnly && dictionary.IsReadOnly)
                        return false;
                    dictionary.Add(entry.Key, entry.Value);
                    return true;
                }

                // 3.) ICollection<T>
                Type collType = collection.GetType();
                if (collType.IsImplementationOfGenericType(Reflector.ICollectionGenType, out Type closedGenericType))
                {
                    if (checkReadOnly && (bool)PropertyAccessor.GetAccessor(closedGenericType.GetProperty(nameof(ICollection<_>.IsReadOnly))).Get(collection))
                        return false;
                    var genericArgument = closedGenericType.GetGenericArguments()[0];
                    if (!genericArgument.CanAcceptValue(item))
                        return false;
                    MethodAccessor.GetAccessor(closedGenericType.GetMethod(nameof(ICollection<_>.Add))).Invoke(collection, item);
                    return true;
                }

#if !NET35
                // 4.) IProducerConsumerCollection<T>
                if (collType.IsImplementationOfGenericType(typeof(IProducerConsumerCollection<>), out closedGenericType))
                    return (bool)MethodAccessor.GetAccessor(closedGenericType.GetMethod(nameof(IProducerConsumerCollection<_>.TryAdd))).Invoke(collection, item);
#endif

#if NET35
                if (list == null)
#endif
                {
                    return false;
                }

#if NET35
                // if we reach this point now we can try to add null to the IList
                list.Add(null);
                return true;
#endif
            }
            catch (Exception)
            {
                if (throwError)
                    throw;
                return false;
            }
        }

        /// <summary>
        /// Tries to remove all elements from the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to clear.</param>
        /// <param name="checkReadOnly"><see langword="true"/> to return <see langword="false"/> if the collection is read-only; <see langword="false"/> to attempt the clearing without checking the read-only state.</param>
        /// <param name="throwError"><see langword="true"/> to forward any exception thrown by the matching clear method; <see langword="false"/> to suppress inner exceptions and return <see langword="false"/> on failure.</param>
        /// <returns><see langword="true"/> if a clear method could be successfully called; <see langword="false"/> if such method was not found, or <paramref name="checkReadOnly"/> is <see langword="true"/> and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/> and the clear method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="collection"/> can be cleared if the collection is either an <see cref="ICollection{T}"/>, <see cref="IList"/> or <see cref="IDictionary"/> implementation.</para>
        /// </remarks>
        public static bool TryClear<T>([NoEnumeration]this IEnumerable<T> collection, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            try
            {
                switch (collection)
                {
                    case ICollection<T> genericCollection:
                        if (checkReadOnly && genericCollection.IsReadOnly)
                            return false;
                        genericCollection.Clear();
                        return true;
                    case IList list:
                        if (checkReadOnly && list.IsReadOnly)
                            return false;
                        list.Clear();
                        return true;
                    case IDictionary dictionary:
                        if (checkReadOnly && dictionary.IsReadOnly)
                            return false;
                        dictionary.Clear();
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                if (throwError)
                    throw;
                return false;
            }
        }

        /// <summary>
        /// Tries to remove all elements from the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to clear.</param>
        /// <param name="checkReadOnly"><see langword="true"/> to return <see langword="false"/> if the collection is read-only; <see langword="false"/> to attempt the clearing without checking the read-only state.</param>
        /// <param name="throwError"><see langword="true"/> to forward any exception thrown by the matching clear method; <see langword="false"/> to suppress inner exceptions and return <see langword="false"/> on failure.</param>
        /// <returns><see langword="true"/> if a clear method could be successfully called; <see langword="false"/> if such method was not found, or <paramref name="checkReadOnly"/> is <see langword="true"/> and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/> and the clear method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="collection"/> can be cleared if the collection is either an <see cref="IList"/>, <see cref="IDictionary"/> or <see cref="ICollection{T}"/> implementation.</para>
        /// </remarks>
        public static bool TryClear([NoEnumeration]this IEnumerable collection, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            try
            {
                switch (collection)
                {
                    case IList list:
                        if (checkReadOnly && list.IsReadOnly)
                            return false;
                        list.Clear();
                        return true;
                    case IDictionary dictionary:
                        if (checkReadOnly && dictionary.IsReadOnly)
                            return false;
                        dictionary.Clear();
                        return true;
                    default:
                        Type collType = collection.GetType();
                        if (collType.IsImplementationOfGenericType(Reflector.ICollectionGenType, out Type closedGenericType))
                        {
                            if (checkReadOnly && (bool)PropertyAccessor.GetAccessor(closedGenericType.GetProperty(nameof(ICollection<_>.IsReadOnly))).Get(collection))
                                return false;
                            MethodAccessor.GetAccessor(closedGenericType.GetMethod(nameof(ICollection<_>.Clear))).Invoke(collection);
                            return true;
                        }

                        return false;
                }
            }
            catch (Exception)
            {
                if (throwError)
                    throw;
                return false;
            }
        }

        #endregion

        #region Lookup

        /// <summary>
        /// Determines whether the specified <paramref name="source"/> is <see langword="null"/>&#160;or empty (has no elements).
        /// </summary>
        /// <param name="source">The source to check.</param>
        /// <returns><see langword="true"/>&#160;if the <paramref name="source"/> collection is <see langword="null"/>&#160;or empty; otherwise, <see langword="false"/>.</returns>
        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            if (source == null)
                return true;

            if (source is ICollection collection)
                return collection.Count == 0;

            var disposableEnumerator = source as IDisposable;
            try
            {
                IEnumerator enumerator = source.GetEnumerator();
                return enumerator.MoveNext();
            }
            finally
            {
                disposableEnumerator?.Dispose();
            }
        }

        /// <summary>
        /// Determines whether the specified <paramref name="source"/> is <see langword="null"/>&#160;or empty (has no elements).
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The source to check.</param>
        /// <returns><see langword="true"/>&#160;if the <paramref name="source"/> collection is <see langword="null"/>&#160;or empty; otherwise, <see langword="false"/>.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source == null)
                return true;

            if (source is ICollection<T> collection)
                return collection.Count == 0;

#if !(NET35 || NET40)
            if (source is IReadOnlyCollection<T> readOnlyCollection)
                return readOnlyCollection.Count == 0;
#endif

            return ((IEnumerable)source).IsNullOrEmpty();
        }

        /// <summary>
        /// Searches for an element in the <paramref name="source"/> enumeration where the specified <paramref name="predicate"/> returns <see langword="true"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the enumeration..</typeparam>
        /// <param name="source">The source enumeration to search.</param>
        /// <param name="predicate">The predicate to use for the search.</param>
        /// <returns>The index of the found element, or -1 if there was no match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.ArgumentNull);
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate), Res.ArgumentNull);

            if (source is IList<T> list)
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
        /// <typeparam name="T">The type of the elements in the enumeration..</typeparam>
        /// <param name="source">The source enumeration to search.</param>
        /// <param name="element">The element to search.</param>
        /// <returns>The index of the found element, or -1 if <paramref name="element"/> was not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static int IndexOf<T>(this IEnumerable<T> source, T element)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.ArgumentNull);

            var comparer = element is Enum ? (IEqualityComparer<T>)EnumComparer<T>.Comparer : EqualityComparer<T>.Default;
            if (source is IList<T> list)
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
            foreach (T item in source)
            {
                if (comparer.Equals(item, element))
                    return index;

                index++;
            }

            return -1;
        }

        #endregion

        #region Conversion

        /// <summary>
        /// Creates a <see cref="CircularList{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to create a <see cref="CircularList{T}"/> from.</param>
        /// <returns>A <see cref="CircularList{T}"/> that contains elements from the input sequence.</returns>
        /// <remarks>
        /// The method forces immediate query evaluation and returns a <see cref="CircularList{T}"/> that contains the query results.
        /// You can append this method to your query in order to obtain a cached copy of the query results.
        /// </remarks>
        public static CircularList<T> ToCircularList<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.ArgumentNull);

            return new CircularList<T>(source);
        }

        #endregion

        #region Random

        /// <summary>
        /// Shuffles an enumerable <paramref name="source"/> (randomizes its elements) using the provided <paramref name="seed"/> with a new <see cref="Random"/> instance.
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
                throw new ArgumentNullException(nameof(random), Res.ArgumentNull);
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.ArgumentNull);

            return source.Select(item => new { Order = random.Next(), Value = item }).OrderBy(i => i.Order).Select(i => i.Value);
        }

        /// <summary>
        /// Gets a random element from the enumerable <paramref name="source"/> using a new <see cref="Random"/> instance.
        /// </summary>
        /// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to select an element from.</param>
        /// <param name="defaultIfEmpty">If <see langword="true"/>&#160;and <paramref name="source"/> is empty, the default value of <typeparamref name="T"/> is returned.
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
        /// <param name="defaultIfEmpty">If <see langword="true"/>&#160;and <paramref name="source"/> is empty, the default value of <typeparamref name="T"/> is returned.
        /// If <see langword="false"/>, and <paramref name="source"/> is empty, an <see cref="ArgumentException"/> will be thrown. This parameter is optional.
        /// <br/>Default value: <see langword="false"/>.</param>
        /// <returns>A random element from the <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="random"/> or <paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="source"/> contains no elements and <paramref name="defaultIfEmpty"/> is <see langword="false"/>.</exception>
        public static T GetRandomElement<T>(this IEnumerable<T> source, Random random, bool defaultIfEmpty = false)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random), Res.ArgumentNull);
            if (source == null)
                throw new ArgumentNullException(nameof(source), Res.ArgumentNull);

            if (source is IList<T> list)
                return list.Count > 0
                    ? list[random.Next(list.Count)]
                    : defaultIfEmpty ? default(T) : throw new ArgumentException(Res.CollectionEmpty, nameof(source));

#if !NET35 && !NET40
            if (source is IReadOnlyList<T> readonlyList)
                return readonlyList.Count > 0
                    ? readonlyList[random.Next(readonlyList.Count)]
                    : defaultIfEmpty ? default(T) : throw new ArgumentException(Res.CollectionEmpty, nameof(source));
#endif

            using (IEnumerator<T> shuffledEnumerator = Shuffle(source, random).GetEnumerator())
            {
                if (!shuffledEnumerator.MoveNext())
                    return defaultIfEmpty ? default(T) : throw new ArgumentException(Res.CollectionEmpty, nameof(source));
                return shuffledEnumerator.Current;
            }
        }

        #endregion

        #endregion

        #region Internal Methods

        /// <summary>
        /// Adjusts the initializer collection created by <see cref="TypeExtensions.CreateInitializerCollection"/> after it is populated before calling the constructor.
        /// </summary>
        internal static IEnumerable AdjustInitializerCollection([NoEnumeration]this IEnumerable initializerCollection, ConstructorInfo collectionCtor)
        {
            Type collectionType = collectionCtor.DeclaringType;

            // Reverse for Stack
            if (typeof(Stack).IsAssignableFrom(collectionType) || collectionType.IsImplementationOfGenericType(typeof(Stack<>))
#if !NET35
                || collectionType.IsImplementationOfGenericType(typeof(ConcurrentStack<>))
#endif
            )
            {
                IList list = (IList)initializerCollection;
                int length = list.Count;
                int to = length / 2;
                for (int i = 0; i < to; i++)
                {
                    object temp = list[i];
                    list[i] = list[length - i - 1];
                    list[length - i - 1] = temp;
                }
            }

            // Converting to array for array ctor parameter
            Type parameterType = collectionCtor.GetParameters()[0].ParameterType;
            if (!parameterType.IsArray)
                return initializerCollection;

            ICollection coll = (ICollection)initializerCollection;

            // ReSharper disable once AssignNullToNotNullAttribute - parameter is an array so will be never null
            Array initializerArray = Array.CreateInstance(parameterType.GetElementType(), coll.Count);
            coll.CopyTo(initializerArray, 0);
            return initializerArray;
        }

        #endregion

        #endregion
    }
}
