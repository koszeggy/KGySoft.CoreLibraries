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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using KGySoft.Annotations;
using KGySoft.Collections;
using KGySoft.Reflection;
#if !NET35
using System.Collections.Concurrent;
#endif

#endregion

#if NET35 || NET40
#pragma warning disable CS1574 // the documentation contains types that are not available in every target
#endif

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="IEnumerable{T}"/> type.
    /// </summary>
    public static class EnumerableExtensions
    {
#pragma warning disable CA1062 // Validate arguments of public methods - false alarm, this class uses ThrowHelper but FxCop does not recognize ContractAnnotationAttribute

        #region Fields

        private static IThreadSafeCacheAccessor<Type, Type> genericEnumerableCache;

        #endregion

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
                Throw.ArgumentNullException(Argument.source);
            if (action == null)
                Throw.ArgumentNullException(Argument.action);

            // ReSharper disable PossibleMultipleEnumeration
            foreach (T item in source)
                action(item);

            return source;
            // ReSharper restore PossibleMultipleEnumeration
        }

        /// <summary>
        /// Tries to add the specified <paramref name="item"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to add the <paramref name="item"/> to.</param>
        /// <param name="item">The item to add.</param>
        /// <param name="checkReadOnly"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only; <see langword="false"/>&#160;to attempt adding the element without checking the read-only state. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found add method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found add method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if an adding method could be successfully called; <see langword="false"/>&#160;if such method was not found, or <paramref name="checkReadOnly"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the adding method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="item"/> can be added to the <paramref name="collection"/> if that is either an <see cref="ICollection{T}"/>, <see cref="IProducerConsumerCollection{T}"/> or <see cref="IList"/> implementation.</para>
        /// </remarks>
        public static bool TryAdd<T>([NoEnumeration]this IEnumerable<T> collection, T item, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

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
                        if (checkReadOnly && (list.IsReadOnly || list.IsFixedSize))
                            return false;
                        list.Add(item);
                        return true;

                    default:
                        // TODO: Reflector.TryRunMethod...
                        return false;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to add the specified <paramref name="item"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to add the <paramref name="item"/> to.</param>
        /// <param name="item">The item to add.</param>
        /// <param name="checkReadOnly"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only; <see langword="false"/>&#160;to attempt adding the element without checking the read-only state. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found add method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found add method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if an adding method could be successfully called; <see langword="false"/>&#160;if such method was not found, or <paramref name="checkReadOnly"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the adding method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="item"/> can be added to the <paramref name="collection"/> if that is either an <see cref="IList"/>, <see cref="IDictionary"/> (when <paramref name="item"/> is a <see cref="DictionaryEntry"/> instance),
        /// <see cref="ICollection{T}"/> or <see cref="IProducerConsumerCollection{T}"/> implementation.</para>
        /// <note>If it is known that the collection implements only the supported generic interfaces, then for better performance use the generic <see cref="TryAdd{T}"><![CDATA[TryAdd<T>]]></see> overload if possible.</note>
        /// </remarks>
        public static bool TryAdd([NoEnumeration]this IEnumerable collection, object item, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                // 1.) IList
#pragma warning disable IDE0019 // Use pattern matching - must be "as" cast due to the second .NET 3.5 part at the end
                IList list = collection as IList;
#pragma warning restore IDE0019 // Use pattern matching
                if (list != null)
                {
                    if (checkReadOnly && (list.IsReadOnly || list.IsFixedSize))
                        return false;
#if NET35
                    // IList with null element: defer because generic collections in .NET 3.5 don't really support null elements of nullable types via non-generic implementation
                    if (item != null)
#endif
                    {
                        list.Add(item);
                        return true;
                    }
                }

                switch (item)
                {
                    // 2.) ICollection<object>
                    case ICollection<object> genericCollection:
                        if (checkReadOnly && genericCollection.IsReadOnly)
                            return false;
                        genericCollection.Add(item);
                        return true;
#if !NET35
                    // 3.) IProducerConsumerCollection<object>
                    case IProducerConsumerCollection<object> producerConsumerCollection:
                        return producerConsumerCollection.TryAdd(item);
#endif
                    // 4.) IDictionary
                    case DictionaryEntry entry when collection is IDictionary dictionary:
                        if (checkReadOnly && dictionary.IsReadOnly)
                            return false;
                        dictionary.Add(entry.Key, entry.Value);
                        return true;
                }

                // 5.) ICollection<T>
                Type collType = collection.GetType();
                if (collType.IsImplementationOfGenericType(Reflector.ICollectionGenType, out Type genericCollectionInterface))
                {
                    if (checkReadOnly && collection.IsReadOnly(genericCollectionInterface))
                        return false;
                    var genericArgument = genericCollectionInterface.GetGenericArguments()[0];
                    if (!genericArgument.CanAcceptValue(item))
                        return false;
                    collection.Add(genericCollectionInterface, item);
                    return true;
                }

#if !NET35
                // 6.) IProducerConsumerCollection<T>
                if (collType.IsImplementationOfGenericType(typeof(IProducerConsumerCollection<>), out genericCollectionInterface))
                    return collection.TryAddToProducerConsumerCollection(genericCollectionInterface, item);
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
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to add the specified <paramref name="collection"/> to the <paramref name="target"/> collection.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collections.</typeparam>
        /// <param name="target">The target collection.</param>
        /// <param name="collection">The collection to add to the <paramref name="target"/>.</param>
        /// <param name="checkReadOnly"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the target collection is read-only; <see langword="false"/>&#160;to attempt adding the <paramref name="collection"/> without checking the read-only state. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found add method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found add method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>, if the whole <paramref name="collection"/> could be added to <paramref name="target"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>The specified <paramref name="collection"/> can be added to the <paramref name="target"/> collection if that is either an <see cref="ICollection{T}"/>, <see cref="IProducerConsumerCollection{T}"/> or <see cref="IList"/> implementation.</para>
        /// <para>If <paramref name="target"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeCollection{T}"/> implementation,
        /// then the elements of <paramref name="collection"/> will only be added one by one.</para>
        /// </remarks>
        public static bool TryAddRange<T>([NoEnumeration]this IEnumerable<T> target, IEnumerable<T> collection, bool checkReadOnly = true, bool throwError = true)
        {
            if (target == null)
                Throw.ArgumentNullException(Argument.target);
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                if (target is ICollection<T> genericCollection)
                {
                    if (checkReadOnly && genericCollection.IsReadOnly)
                        return false;
                    genericCollection.AddRange(collection);
                    return true;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            return collection.All(item => target.TryAdd(item, checkReadOnly, throwError));
        }

        /// <summary>
        /// Tries to add the specified <paramref name="collection"/> to the <paramref name="target"/> collection.
        /// </summary>
        /// <param name="target">The target collection.</param>
        /// <param name="collection">The collection to add to the <paramref name="target"/>.</param>
        /// <param name="checkReadOnly"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the target collection is read-only; <see langword="false"/>&#160;to attempt adding the <paramref name="collection"/> without checking the read-only state. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found add method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found add method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>, if the whole <paramref name="collection"/> could be added to <paramref name="target"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>The <paramref name="collection"/> can be added to the <paramref name="target"/> collection if that is either an <see cref="ICollection{T}"/>, <see cref="IProducerConsumerCollection{T}"/> or <see cref="IList"/> implementation.</para>
        /// <para>If <paramref name="target"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeCollection{T}"/> implementation,
        /// then the elements of <paramref name="collection"/> will only be added one by one.</para>
        /// <note>Whenever possible, try to use the generic <see cref="TryAddRange{T}"><![CDATA[TryAddRange<T>]]></see> overload for better performance.</note>
        /// <note type="warning">If not every element in <paramref name="collection"/> is compatible with <paramref name="target"/>, then it can happen that some elements
        /// of <paramref name="collection"/> have been added to <paramref name="target"/> and the method returns <see langword="false"/>.</note>
        /// </remarks>
        public static bool TryAddRange([NoEnumeration]this IEnumerable target, IEnumerable collection, bool checkReadOnly = true, bool throwError = true)
        {
            if (target == null)
                Throw.ArgumentNullException(Argument.target);
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (target)
                {
                    case ICollection<object> genericCollection:
                        if (checkReadOnly && genericCollection.IsReadOnly)
                            return false;
                        genericCollection.AddRange(collection.Cast<object>());
                        return true;
                    default:
                        // ICollection<T>: CollectionExtensions.AddRange<T>
                        if (target.GetType().IsImplementationOfGenericType(Reflector.ICollectionGenType, out Type genericCollectionInterface))
                        {
                            Type t = genericCollectionInterface.GetGenericArguments()[0];
                            if (!collection.IsGenericEnumerableOf(t))
                                break;

                            if (checkReadOnly && target.IsReadOnly(genericCollectionInterface))
                                return false;
                            target.AddRange(t, collection);
                            return true;
                        }

                        break;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            return collection.Cast<object>().All(item => target.TryAdd(item, checkReadOnly, throwError));
        }

        /// <summary>
        /// Tries to remove all elements from the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to clear.</param>
        /// <param name="checkReadOnly"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only; <see langword="false"/>&#160;to attempt the clearing without checking the read-only state. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by the matching clear method; <see langword="false"/>&#160;to suppress inner exceptions and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if a clear method could be successfully called; <see langword="false"/>&#160;if such method was not found, or <paramref name="checkReadOnly"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the clear method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="collection"/> can be cleared if that is either an <see cref="ICollection{T}"/>, <see cref="IList"/> or <see cref="IDictionary"/> implementation.</para>
        /// </remarks>
        public static bool TryClear<T>([NoEnumeration]this IEnumerable<T> collection, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

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
                        if (checkReadOnly && (list.IsReadOnly || list.IsFixedSize))
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
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to remove all elements from the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to clear.</param>
        /// <param name="checkReadOnly"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only; <see langword="false"/>&#160;to attempt the clearing without checking the read-only state. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by the matching clear method; <see langword="false"/>&#160;to suppress inner exceptions and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if a clear method could be successfully called; <see langword="false"/>&#160;if such method was not found, or <paramref name="checkReadOnly"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the clear method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="collection"/> can be cleared if that is either an <see cref="IList"/>, <see cref="IDictionary"/> or <see cref="ICollection{T}"/> implementation.</para>
        /// <note>If it is known that the collection implements only the supported generic <see cref="ICollection{T}"/> interface, then for better performance use the generic <see cref="TryClear{T}"><![CDATA[TryClear<T>]]></see> overload if possible.</note>
        /// </remarks>
        public static bool TryClear([NoEnumeration]this IEnumerable collection, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (collection)
                {
                    case IList list:
                        if (checkReadOnly && (list.IsReadOnly || list.IsFixedSize))
                            return false;
                        list.Clear();
                        return true;
                    case IDictionary dictionary:
                        if (checkReadOnly && dictionary.IsReadOnly)
                            return false;
                        dictionary.Clear();
                        return true;
                    case ICollection<object> genericCollection:
                        if (checkReadOnly && genericCollection.IsReadOnly)
                            return false;
                        genericCollection.Clear();
                        return true;
                    default:
                        if (collection.GetType().IsImplementationOfGenericType(Reflector.ICollectionGenType, out Type genericCollectionInterface))
                        {
                            if (checkReadOnly && collection.IsReadOnly(genericCollectionInterface))
                                return false;
                            collection.Clear(genericCollectionInterface);
                            return true;
                        }

                        return false;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to insert the specified <paramref name="item"/> at the specified <paramref name="index"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to insert the <paramref name="item"/> into.</param>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to be inserted.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt inserting the element without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found insert method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found insert method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if an inserting method could be successfully called; <see langword="false"/>&#160;if such method was not found, or <paramref name="checkReadOnlyAndBounds"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the inserting method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="item"/> can be inserted into the <paramref name="collection"/> if that is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// </remarks>
        public static bool TryInsert<T>([NoEnumeration]this IEnumerable<T> collection, int index, T item, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (collection)
                {
                    case IList<T> genericList:
                        if (checkReadOnlyAndBounds && (genericList.IsReadOnly || index < 0 || index > genericList.Count))
                            return false;
                        genericList.Insert(index, item);
                        return true;

                    case IList list:
                        if (checkReadOnlyAndBounds && (list.IsReadOnly || list.IsFixedSize || index < 0 || index > list.Count))
                            return false;
                        list.Insert(index, item);
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to insert the specified <paramref name="item"/> at the specified <paramref name="index"/> to the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to insert the <paramref name="item"/> into.</param>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to be inserted.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt inserting the element without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found insert method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found insert method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if an inserting method could be successfully called; <see langword="false"/>&#160;if such method was not found, or <paramref name="checkReadOnlyAndBounds"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the inserting method threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="item"/> can be inserted into the <paramref name="collection"/> if that is either an <see cref="IList"/> or <see cref="IList{T}"/> implementation.</para>
        /// <note>If it is known that the collection implements only the supported generic <see cref="IList{T}"/> interface, then for better performance use the generic <see cref="TryInsert{T}"><![CDATA[TryInsert<T>]]></see> overload if possible.</note>
        /// </remarks>
        public static bool TryInsert([NoEnumeration]this IEnumerable collection, int index, object item, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
#pragma warning disable IDE0019 // Use pattern matching - must be "as" cast due to the second .NET 3.5 part at the end
                IList list = collection as IList;
#pragma warning restore IDE0019 // Use pattern matching
                if (list != null)
                {
                    if (checkReadOnlyAndBounds && (list.IsReadOnly || list.IsFixedSize || index < 0 || index > list.Count))
                        return false;
#if NET35
                    // IList with null element: defer because generic collections in .NET 3.5 don't really support null elements of nullable types via non-generic implementation
                    if (item != null)
#endif
                    {
                        list.Insert(index, item);
                        return true;
                    }
                }

                if (collection is IList<object> genericList)
                {
                    if (checkReadOnlyAndBounds && (genericList.IsReadOnly || index < 0 || index > genericList.Count))
                        return false;
                    genericList.Insert(index, item);
                    return true;
                }

                if (collection.GetType().IsImplementationOfGenericType(Reflector.IListGenType, out Type genericListInterface))
                {
                    var genericArgument = genericListInterface.GetGenericArguments()[0];
                    if (!genericArgument.CanAcceptValue(item))
                        return false;

                    if (checkReadOnlyAndBounds)
                    {
                        if (index < 0)
                            return false;
                        Type genericCollectionInterface = genericListInterface.GetInterface(Reflector.ICollectionGenType.Name);
                        int count = collection is ICollection coll ? coll.Count : collection.Count(genericCollectionInterface);
                        if (index > count || collection.IsReadOnly(genericCollectionInterface))
                            return false;
                    }

                    collection.Insert(genericListInterface, index, item);
                    return true;
                }

#if NET35
                if (list == null)
#endif
                {
                    return false;
                }

#if NET35
                // if we reach this point now we can try to insert null to the IList
                list.Insert(index, null);
                return true;
#endif
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to insert the specified <paramref name="collection"/> into the <paramref name="target"/> collection.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collections.</typeparam>
        /// <param name="target">The target collection.</param>
        /// <param name="index">The zero-based index at which the <paramref name="collection"/> should be inserted.</param>
        /// <param name="collection">The collection to insert into the <paramref name="target"/>.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the target collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt inserting the <paramref name="collection"/> without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found insert method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found insert method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>, if the whole <paramref name="collection"/> could be inserted into <paramref name="target"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>The specified <paramref name="collection"/> can be inserted in the <paramref name="target"/> collection if that is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// <para>If <paramref name="target"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeCollection{T}"/> implementation,
        /// then the elements of <paramref name="collection"/> will only be inserted one by one.</para>
        /// </remarks>
        public static bool TryInsertRange<T>([NoEnumeration]this IEnumerable<T> target, int index, IEnumerable<T> collection, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (target == null)
                Throw.ArgumentNullException(Argument.target);
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                if (target is IList<T> genericList)
                {
                    if (checkReadOnlyAndBounds && (genericList.IsReadOnly || index < 0 || index > genericList.Count))
                        return false;
                    genericList.InsertRange(index, collection);
                    return true;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            return collection.All(item => target.TryInsert(index++, item, checkReadOnlyAndBounds, throwError));
        }

        /// <summary>
        /// Tries to insert the specified <paramref name="collection"/> into the <paramref name="target"/> collection.
        /// </summary>
        /// <param name="target">The target collection.</param>
        /// <param name="index">The zero-based index at which the <paramref name="collection"/> should be inserted.</param>
        /// <param name="collection">The collection to insert into the <paramref name="target"/>.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the target collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt inserting the <paramref name="collection"/> without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found insert method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found insert method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>, if the whole <paramref name="collection"/> could be inserted into <paramref name="target"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>The specified <paramref name="collection"/> can be inserted in the <paramref name="target"/> collection if that is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// <para>If <paramref name="target"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeCollection{T}"/> implementation,
        /// then the elements of <paramref name="collection"/> will only be inserted one by one.</para>
        /// <note>Whenever possible, try to use the generic <see cref="TryInsertRange{T}"><![CDATA[TryInsertRange<T>]]></see> overload for better performance.</note>
        /// <note type="warning">If not every element in <paramref name="collection"/> is compatible with <paramref name="target"/>, then it can happen that some elements
        /// of <paramref name="collection"/> have been added to <paramref name="target"/> and the method returns <see langword="false"/>.</note>
        /// </remarks>
        public static bool TryInsertRange([NoEnumeration]this IEnumerable target, int index, IEnumerable collection, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (target == null)
                Throw.ArgumentNullException(Argument.target);
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (target)
                {
                    case IList<object> genericList:
                        {
                            if (checkReadOnlyAndBounds && (genericList.IsReadOnly || index < 0 || index > genericList.Count))
                                return false;
                            genericList.InsertRange(index, collection.Cast<object>());
                            return true;
                        }
                    default:
                        // IList<T>: ListExtensions.InsertRange<T>
                        if (target.GetType().IsImplementationOfGenericType(Reflector.IListGenType, out Type genericListInterface))
                        {
                            Type t = genericListInterface.GetGenericArguments()[0];
                            if (!collection.IsGenericEnumerableOf(t))
                                break;

                            if (checkReadOnlyAndBounds)
                            {
                                if (index < 0)
                                    return false;
                                Type genericCollectionInterface = genericListInterface.GetInterface(Reflector.ICollectionGenType.Name);
                                int count = target is ICollection coll ? coll.Count : target.Count(genericCollectionInterface);
                                if (index > count || target.IsReadOnly(genericCollectionInterface))
                                    return false;
                            }

                            target.InsertRange(t, index, collection);
                            return true;
                        }

                        break;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            return collection.Cast<object>().All(item => target.TryInsert(index++, item, checkReadOnlyAndBounds, throwError));
        }

        /// <summary>
        /// Tries to remove the specified <paramref name="item"/> from to the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to remove the <paramref name="item"/> from.</param>
        /// <param name="item">The item to be removed.</param>
        /// <param name="checkReadOnly"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only; <see langword="false"/>&#160;to attempt removing the element without checking the read-only state. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found remove method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found remove method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> could be successfully removed; <see langword="false"/>&#160;if a removing method was not found or the <paramref name="item"/> could not be removed, <paramref name="checkReadOnly"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// <paramref name="throwError"/> is <see langword="false"/>&#160;and the removing method threw an exception, or the removing method returned <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>Removal is supported if <paramref name="collection"/> is either an <see cref="ICollection{T}"/> or <see cref="IList"/> implementation.</para>
        /// </remarks>
        public static bool TryRemove<T>([NoEnumeration]this IEnumerable<T> collection, T item, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (collection)
                {
                    case ICollection<T> genericCollection:
                        if (checkReadOnly && genericCollection.IsReadOnly)
                            return false;
                        return genericCollection.Remove(item);

                    case IList list:
                        if (checkReadOnly && (list.IsReadOnly || list.IsFixedSize))
                            return false;
                        int index = list.IndexOf(item);
                        if (index < 0)
                            return false;
                        list.RemoveAt(index);
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to remove the specified <paramref name="item"/> from to the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to remove the <paramref name="item"/> from.</param>
        /// <param name="item">The item to be removed.</param>
        /// <param name="checkReadOnly"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only; <see langword="false"/>&#160;to attempt removing the element without checking the read-only state. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found remove method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found remove method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> could be successfully removed; <see langword="false"/>&#160;if a removing method was not found or the <paramref name="item"/> could not be removed, <paramref name="checkReadOnly"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// <paramref name="throwError"/> is <see langword="false"/>&#160;and the removing method threw an exception, or the removing method returned <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>Removal is supported if <paramref name="collection"/> is either an <see cref="IList"/> or <see cref="ICollection{T}"/> implementation.</para>
        /// <note>If it is known that the collection implements only the supported generic <see cref="ICollection{T}"/> interface, then for better performance use the generic <see cref="TryRemove{T}"><![CDATA[TryRemove<T>]]></see> overload if possible.</note>
        /// </remarks>
        public static bool TryRemove([NoEnumeration]this IEnumerable collection, object item, bool checkReadOnly = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
#pragma warning disable IDE0019 // Use pattern matching - must be "as" cast due to the second .NET 3.5 part at the end
                IList list = collection as IList;
#pragma warning restore IDE0019 // Use pattern matching
                if (list != null)
                {
                    if (checkReadOnly && (list.IsReadOnly || list.IsFixedSize))
                        return false;

#if NET35
                    // IList with null element: defer because generic collections in .NET 3.5 don't really support null elements of nullable types via non-generic implementation
                    if (item != null)
#endif
                    {
                        int index = list.IndexOf(item);
                        if (index < 0)
                            return false;
                        list.RemoveAt(index);
                        return true;
                    }
                }

                if (collection is ICollection<object> genericCollection)
                {
                    if (checkReadOnly && genericCollection.IsReadOnly)
                        return false;
                    return genericCollection.Remove(item);
                }

                if (collection.GetType().IsImplementationOfGenericType(Reflector.ICollectionGenType, out Type genericCollectionInterface))
                {
                    if (checkReadOnly && collection.IsReadOnly(genericCollectionInterface))
                        return false;
                    var genericArgument = genericCollectionInterface.GetGenericArguments()[0];
                    if (!genericArgument.CanAcceptValue(item))
                        return false;
                    return collection.Remove(genericCollectionInterface, item);
                }

#if NET35
                if (list == null)
#endif
                {
                    return false;
                }

#if NET35
                // if we reach this point now we can try to remove null from the IList
                int indexOfNull = list.IndexOf(null);
                if (indexOfNull < 0)
                    return false;
                list.RemoveAt(indexOfNull);
                return true;
#endif
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to remove an item at the specified <paramref name="index"/> from the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to remove the item from.</param>
        /// <param name="index">The zero-based index of the item to be removed.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt removing the element without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found remove method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found remove method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if a remove method could be successfully called; <see langword="false"/>&#160;if such method was not found, or <paramref name="checkReadOnlyAndBounds"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the removing method threw an exception.</returns>
        /// <remarks>
        /// <para>Removal is supported if <paramref name="collection"/> is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// </remarks>
        public static bool TryRemoveAt<T>([NoEnumeration]this IEnumerable<T> collection, int index, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (collection)
                {
                    case IList<T> genericList:
                        if (checkReadOnlyAndBounds && (genericList.IsReadOnly || index < 0 || index >= genericList.Count))
                            return false;
                        genericList.RemoveAt(index);
                        return true;

                    case IList list:
                        if (checkReadOnlyAndBounds && (list.IsReadOnly || list.IsFixedSize || index < 0 || index >= list.Count))
                            return false;
                        list.RemoveAt(index);
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to remove an item at the specified <paramref name="index"/> from the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to remove the item from.</param>
        /// <param name="index">The zero-based index of the item to be removed.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt removing the element without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found remove method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found remove method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if a remove method could be successfully called; <see langword="false"/>&#160;if such method was not found, or <paramref name="checkReadOnlyAndBounds"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the removing method threw an exception.</returns>
        /// <remarks>
        /// <para>Removal is supported if <paramref name="collection"/> is either an <see cref="IList"/> or <see cref="IList{T}"/> implementation.</para>
        /// <note>If it is known that the collection implements only the supported generic <see cref="IList{T}"/> interface, then for better performance use the generic <see cref="TryRemoveAt{T}"><![CDATA[TryRemoveAt<T>]]></see> overload if possible.</note>
        /// </remarks>
        public static bool TryRemoveAt([NoEnumeration]this IEnumerable collection, int index, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (collection)
                {
                    case IList list:
                        if (checkReadOnlyAndBounds && (list.IsReadOnly || list.IsFixedSize || index < 0 || index >= list.Count))
                            return false;
                        list.RemoveAt(index);
                        return true;
                    case IList<object> genericList:
                        if (checkReadOnlyAndBounds && (genericList.IsReadOnly || index < 0 || index >= genericList.Count))
                            return false;
                        genericList.RemoveAt(index);
                        return true;
                }

                if (collection.GetType().IsImplementationOfGenericType(Reflector.IListGenType, out Type genericListInterface))
                {
                    if (checkReadOnlyAndBounds)
                    {
                        if (index < 0)
                            return false;
                        Type genericCollectionInterface = genericListInterface.GetInterface(Reflector.ICollectionGenType.Name);
                        int count = collection is ICollection coll ? coll.Count : collection.Count(genericCollectionInterface);
                        if (index >= count || collection.IsReadOnly(genericCollectionInterface))
                            return false;
                    }

                    collection.RemoveAt(genericListInterface, index);
                    return true;
                }

                return false;
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to remove <paramref name="count"/> amount of items from the specified <paramref name="collection"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collections.</typeparam>
        /// <param name="collection">The collection to remove the elements from.</param>
        /// <param name="index">The zero-based index of the first item to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the target collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt inserting the <paramref name="collection"/> without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found remove method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found remove method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>, if the whole range could be removed from <paramref name="collection"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>Removal is supported if <paramref name="collection"/> is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// <note>If <paramref name="collection"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeList{T}"/> implementation,
        /// then the elements will only be removed one by one.</note>
        /// </remarks>
        public static bool TryRemoveRange<T>([NoEnumeration]this IEnumerable<T> collection, int index, int count, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);

            try
            {
                if (collection is IList<T> genericList)
                {
                    if (checkReadOnlyAndBounds && (genericList.IsReadOnly || (uint)index >= (uint)genericList.Count || index + count > genericList.Count))
                        return false;
                    genericList.RemoveRange(index, count);
                    return true;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!collection.TryRemoveAt(index, checkReadOnlyAndBounds, throwError))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to remove <paramref name="count"/> amount of items from the specified <paramref name="collection"/> at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="collection">The collection to remove the elements from.</param>
        /// <param name="index">The zero-based index of the first item to remove.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the target collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt inserting the <paramref name="collection"/> without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found remove method; <see langword="false"/>&#160;to suppress the exceptions thrown by the found remove method and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>, if the whole range could be removed from <paramref name="collection"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>Removal is supported if <paramref name="collection"/> is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// <note>If <paramref name="collection"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeList{T}"/> implementation,
        /// then the elements will only be removed one by one.</note>
        /// <note>Whenever possible, try to use the generic <see cref="TryRemoveRange{T}"><![CDATA[TryRemoveRange<T>]]></see> overload for better performance.</note>
        /// </remarks>
        public static bool TryRemoveRange([NoEnumeration]this IEnumerable collection, int index, int count, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);
            if (count < 0)
                Throw.ArgumentOutOfRangeException(Argument.count);

            try
            {
                switch (collection)
                {
                    case IList<object> genericList:
                        if (checkReadOnlyAndBounds && (genericList.IsReadOnly || (uint)index >= (uint)genericList.Count || index + count > genericList.Count))
                            return false;
                        genericList.RemoveRange(index, count);
                        return true;
                    default:
                        // IList<T>: ListExtensions.RemoveRange<T>
                        if (collection.GetType().IsImplementationOfGenericType(Reflector.IListGenType, out Type genericListInterface))
                        {
                            if (checkReadOnlyAndBounds)
                            {
                                if (index < 0)
                                    return false;
                                Type genericCollectionInterface = genericListInterface.GetInterface(Reflector.ICollectionGenType.Name);
                                int collCount = collection is ICollection coll ? coll.Count : collection.Count(genericCollectionInterface);
                                if ((uint)index >= (uint)collCount || index + count > collCount || collection.IsReadOnly(genericCollectionInterface))
                                    return false;
                            }

                            collection.RemoveRange(genericListInterface.GetGenericArguments()[0], index, count);
                            return true;
                        }

                        break;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!collection.TryRemoveAt(index, checkReadOnlyAndBounds, throwError))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Tries to set the specified <paramref name="item"/> at the specified <paramref name="index"/> in the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to set the <paramref name="item"/> in.</param>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be set.</param>
        /// <param name="item">The item to be set.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt setting the element without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found setting member; <see langword="false"/>&#160;to suppress the exceptions thrown by the found setting member and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if a setting member could be successfully called; <see langword="false"/>&#160;if such member was not found, or <paramref name="checkReadOnlyAndBounds"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the setting member threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="item"/> can be set in the <paramref name="collection"/> if that is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// </remarks>
        public static bool TrySetElementAt<T>([NoEnumeration]this IEnumerable<T> collection, int index, T item, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (collection)
                {
                    case IList<T> genericList:
                        if (checkReadOnlyAndBounds && ((!(collection is T[]) && genericList.IsReadOnly) || index < 0 || index >= genericList.Count))
                            return false;
                        genericList[index] = item;
                        return true;

                    case IList list:
                        if (checkReadOnlyAndBounds && (list.IsReadOnly || index < 0 || index > list.Count))
                            return false;
                        list[index] = item;
                        return true;

                    default:
                        return false;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to set the specified <paramref name="item"/> at the specified <paramref name="index"/> in the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to set the <paramref name="item"/> in.</param>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be set.</param>
        /// <param name="item">The item to be set.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt setting the element without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found setting member; <see langword="false"/>&#160;to suppress the exceptions thrown by the found setting member and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if a setting member could be successfully called; <see langword="false"/>&#160;if such member was not found, or <paramref name="checkReadOnlyAndBounds"/> is <see langword="true"/>&#160;and the collection was read-only,
        /// or <paramref name="throwError"/> is <see langword="false"/>&#160;and the setting member threw an exception.</returns>
        /// <remarks>
        /// <para>The <paramref name="item"/> can be set in the <paramref name="collection"/> if that is either an <see cref="IList"/> or <see cref="IList{T}"/> implementation.</para>
        /// <note>If it is known that the collection implements only the supported generic <see cref="IList{T}"/> interface, then for better performance use the generic <see cref="TrySetElementAt{T}"><![CDATA[TrySetElementAt<T>]]></see> overload if possible.</note>
        /// <note>This method returns <see langword="false"/>&#160;also for multidimensional arrays.</note>
        /// </remarks>
        public static bool TrySetElementAt([NoEnumeration]this IEnumerable collection, int index, object item, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
#pragma warning disable IDE0019 // Use pattern matching - must be "as" cast due to the second .NET 3.5 part at the end
                IList list = collection as IList;
#pragma warning restore IDE0019 // Use pattern matching
                if (list != null)
                {
                    if (checkReadOnlyAndBounds && (list.IsReadOnly || index < 0 || index >= list.Count || list is Array array && array.Rank != 1))
                        return false;
#if NET35
                    // IList with null element: defer because generic collections in .NET 3.5 don't really support null elements of nullable types via non-generic implementation
                    if (item != null)
#endif
                    {
                        list[index] = item;
                        return true;
                    }
                }

                if (collection is IList<object> genericList)
                {
                    if (checkReadOnlyAndBounds && ((
#if NET35
                        !(collection is object[]) && // as we skip null above we can reach this point with an array in .NET 3.5, which is ReadOnly as IList<T>
#endif
                        genericList.IsReadOnly) || index < 0 || index >= genericList.Count))
                    {
                        return false;
                    }

                    genericList[index] = item;
                    return true;
                }

                if (collection.GetType().IsImplementationOfGenericType(Reflector.IListGenType, out Type genericListInterface))
                {
                    var genericArgument = genericListInterface.GetGenericArguments()[0];
                    if (!genericArgument.CanAcceptValue(item))
                        return false;

                    if (checkReadOnlyAndBounds)
                    {
                        if (index < 0)
                            return false;
                        Type genericCollectionInterface = genericListInterface.GetInterface(Reflector.ICollectionGenType.Name);
                        int count = collection is ICollection coll ? coll.Count : collection.Count(genericCollectionInterface);
                        if (index >= count || (
#if NET35
                            !(collection is Array) && 
#endif
                            collection.IsReadOnly(genericCollectionInterface)))
                        {
                            return false;
                        }
                    }

                    collection.SetElementAt(genericListInterface, index, item);
                    return true;
                }

#if NET35
                if (list == null)
#endif
                {
                    return false;
                }

#if NET35
                // if we reach this point now we can try to set null to the IList
                list[index] = null;
                return true;
#endif
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to remove <paramref name="count"/> amount of items from the <paramref name="target"/> at the specified <paramref name="index"/>, and
        /// to insert the specified <paramref name="collection"/> at the same position. The number of elements in <paramref name="collection"/> can be different from the amount of removed items.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collections.</typeparam>
        /// <param name="target">The target collection.</param>
        /// <param name="index">The zero-based index of the first item to remove and also the index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <param name="collection">The collection to insert into the <paramref name="target"/> list.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the target collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt inserting the <paramref name="collection"/> without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by the used modifier members; <see langword="false"/>&#160;to suppress the exceptions thrown by the used members and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>, if the whole range could be removed and <paramref name="collection"/> could be inserted into <paramref name="target"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>The replacement can be performed if the <paramref name="target"/> collection is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// <para>If <paramref name="target"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeCollection{T}"/> implementation,
        /// then the elements will only be replaced one by one.</para>
        /// </remarks>
        public static bool TryReplaceRange<T>([NoEnumeration]this IEnumerable<T> target, int index, int count, IEnumerable<T> collection, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (count == 0)
                return target.TryInsertRange(index, collection, checkReadOnlyAndBounds, throwError);

            if (target == null)
                Throw.ArgumentNullException(Argument.target);
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                if (target is IList<T> genericList)
                {
                    if (checkReadOnlyAndBounds && (genericList.IsReadOnly || (uint)index >= (uint)genericList.Count || index + count > genericList.Count))
                        return false;
                    genericList.ReplaceRange(index, count, collection);
                    return true;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                // Copying elements while possible
                int elementsCopied = 0;
                while (count > 0 && enumerator.MoveNext())
                {
                    if (!target.TrySetElementAt(index + elementsCopied++, enumerator.Current, checkReadOnlyAndBounds, throwError))
                        return false;
                    count -= 1;
                }

                // all inserted, removing the rest
                if (count > 0)
                    return target.TryRemoveRange(index + elementsCopied, count, checkReadOnlyAndBounds, throwError);

                // all removed (overwritten), inserting the rest
                IList<T> rest = collection is IList<T> list ? new ListSegment<T>(list, elementsCopied) : enumerator.RestToList();
                if (rest.Count > 0)
                    return target.TryInsertRange(index + elementsCopied, rest, checkReadOnlyAndBounds, throwError);

                // elements to replace had the same size
                return true;
            }
        }

        /// <summary>
        /// Tries to remove <paramref name="count"/> amount of items from the <paramref name="target"/> at the specified <paramref name="index"/>, and
        /// to insert the specified <paramref name="collection"/> at the same position. The number of elements in <paramref name="collection"/> can be different from the amount of removed items.
        /// </summary>
        /// <param name="target">The target collection.</param>
        /// <param name="index">The zero-based index of the first item to remove and also the index at which <paramref name="collection"/> items should be inserted.</param>
        /// <param name="count">The number of items to remove.</param>
        /// <param name="collection">The collection to insert into the <paramref name="target"/> list.</param>
        /// <param name="checkReadOnlyAndBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the target collection is read-only or the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt inserting the <paramref name="collection"/> without checking the read-only state and bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by the used modifier members; <see langword="false"/>&#160;to suppress the exceptions thrown by the used members and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>, if the whole range could be removed and <paramref name="collection"/> could be inserted into <paramref name="target"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>The replacement can be performed if the <paramref name="target"/> collection is either an <see cref="IList{T}"/> or <see cref="IList"/> implementation.</para>
        /// <para>If <paramref name="target"/> is neither a <see cref="List{T}"/> nor an <see cref="ISupportsRangeCollection{T}"/> implementation,
        /// then the elements will only be replaced one by one.</para>
        /// <note>Whenever possible, try to use the generic <see cref="TryReplaceRange{T}"><![CDATA[TryReplaceRange<T>]]></see> overload for better performance.</note>
        /// <note type="warning">If not every element in <paramref name="collection"/> is compatible with <paramref name="target"/>, then it can happen that some elements
        /// of <paramref name="collection"/> have been added to <paramref name="target"/> and the method returns <see langword="false"/>.</note>
        /// </remarks>
        public static bool TryReplaceRange([NoEnumeration]this IEnumerable target, int index, int count, IEnumerable collection, bool checkReadOnlyAndBounds = true, bool throwError = true)
        {
            if (count == 0)
                return target.TryInsertRange(index, collection, checkReadOnlyAndBounds, throwError);

            if (target == null)
                Throw.ArgumentNullException(Argument.target);
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);

            try
            {
                switch (target)
                {
                    case IList<object> genericList:
                        if (checkReadOnlyAndBounds && (genericList.IsReadOnly || (uint)index >= (uint)genericList.Count || index + count > genericList.Count))
                            return false;
                        genericList.ReplaceRange(index, count, collection.Cast<object>());
                        return true;
                    default:
                        // IList<T>: ListExtensions.ReplaceRange<T>
                        if (target.GetType().IsImplementationOfGenericType(Reflector.IListGenType, out Type genericListInterface))
                        {
                            Type t = genericListInterface.GetGenericArguments()[0];
                            if (!collection.IsGenericEnumerableOf(t))
                                break;
                            if (checkReadOnlyAndBounds)
                            {
                                if (index < 0)
                                    return false;
                                Type genericCollectionInterface = genericListInterface.GetInterface(Reflector.ICollectionGenType.Name);
                                int targetCount = target is ICollection coll ? coll.Count : target.Count(genericCollectionInterface);
                                if ((uint)index >= (uint)targetCount || index + count > targetCount || target.IsReadOnly(genericCollectionInterface))
                                    return false;
                            }

                            target.ReplaceRange(t, index, count, collection);
                            return true;
                        }

                        break;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            // One by one
            return TryReplaceRangeDefault(target, index, count, collection, checkReadOnlyAndBounds, throwError);
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
                Throw.ArgumentNullException(Argument.source);
            if (predicate == null)
                Throw.ArgumentNullException(Argument.predicate);

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

                index += 1;
            }

            return -1;
        }

        /// <summary>
        /// Searches for an element in the <paramref name="source"/> enumeration where the specified <paramref name="predicate"/> returns <see langword="true"/>.
        /// </summary>
        /// <param name="source">The source enumeration to search.</param>
        /// <param name="predicate">The predicate to use for the search.</param>
        /// <returns>The index of the found element, or -1 if there was no match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.</exception>
        public static int IndexOf(this IEnumerable source, Func<object, bool> predicate)
        {
            if (source == null)
                Throw.ArgumentNullException(Argument.source);
            
            switch (source)
            {
                case IEnumerable<object> enumerableGeneric:
                    return enumerableGeneric.IndexOf(predicate);
                default:
                    return source.Cast<object>().IndexOf(predicate);
            }
        }

        /// <summary>
        /// Searches for an element in the <paramref name="source"/> enumeration.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the enumeration..</typeparam>
        /// <param name="source">The source enumeration to search.</param>
        /// <param name="element">The element to search.</param>
        /// <returns>The index of the found element, or -1 if <paramref name="element"/> was not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "False alarm: see the null case")]
        public static int IndexOf<T>(this IEnumerable<T> source, T element)
        {
            if (source == null)
                Throw.ArgumentNullException(Argument.source);

            switch (source)
            {
                case IList<T> genericList:
                    return genericList.IndexOf(element);
                case IList list:
                    return list.IndexOf(element);
                default:
                    var comparer = ComparerHelper<T>.EqualityComparer;
                    var index = 0;
                    foreach (T item in source)
                    {
                        if (comparer.Equals(item, element))
                            return index;

                        index += 1;
                    }

                    return -1;
            }
        }

        /// <summary>
        /// Searches for an element in the <paramref name="source"/> enumeration.
        /// </summary>
        /// <param name="source">The source enumeration to search.</param>
        /// <param name="element">The element to search.</param>
        /// <returns>The index of the found element, or -1 if <paramref name="element"/> was not found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        public static int IndexOf(this IEnumerable source, object element)
        {
            if (source == null)
                Throw.ArgumentNullException(Argument.source);

            switch (source)
            {
                case IList<object> genericList:
                    return genericList.IndexOf(element);
                case IList list:
                    return list.IndexOf(element);
                default:
                    return source.Cast<object>().IndexOf(element);
            }
        }

        /// <summary>
        /// Tries to get an <paramref name="item"/> at the specified <paramref name="index"/> in the <paramref name="collection"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the <paramref name="collection"/>.</typeparam>
        /// <param name="collection">The collection to retrieve the <paramref name="item"/> from.</param>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be returned.</param>
        /// <param name="item">If this method returns <see langword="true"/>, then this parameter contains the found item.</param>
        /// <param name="checkBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt getting the element via the possible interfaces without checking bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found getting member; <see langword="false"/>&#160;to suppress the exceptions thrown by the found getting member and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> could be retrieved; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>If <paramref name="collection"/> is neither an <see cref="IList{T}"/>, <see cref="IReadOnlyList{T}"/>, nor an <see cref="IList"/> implementation, then the <paramref name="collection"/> will be iterated.
        /// In this case the <paramref name="checkBounds"/> argument is ignored and the method returns <see langword="false"/>&#160;if the <paramref name="index"/> is invalid.</para>
        /// <note>This method is similar to the <see cref="Enumerable.ElementAtOrDefault{TSource}">Enumerable.ElementAtOrDefault</see> method. The main difference is that if <see cref="Enumerable.ElementAtOrDefault{TSource}">Enumerable.ElementAtOrDefault</see>
        /// returns the default value of <typeparamref name="T"/>, then it cannot be known whether the returned item existed in the collection at the specified position.</note>
        /// </remarks>
        public static bool TryGetElementAt<T>(this IEnumerable<T> collection, int index, out T item, bool checkBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);
            item = default;

            try
            {
                switch (collection)
                {
                    case IList<T> genericList:
                        if (checkBounds && (index < 0 || index >= genericList.Count))
                            return false;
                        item = genericList[index];
                        return true;

#if !(NET35 || NET40)
                    case IReadOnlyList<T> readOnlyList:
                        if (checkBounds && (index < 0 || index >= readOnlyList.Count))
                            return false;
                        item = readOnlyList[index];
                        return true;
#endif

                    case IList list:
                        if (checkBounds && (index < 0 || index > list.Count))
                            return false;
                        object result = list[index];
                        if (result is T t)
                        {
                            item = t;
                            return true;
                        }

                        return false;

                    default:
                        if (index < 0)
                            return false;
                        using (IEnumerator<T> enumerator = collection.GetEnumerator())
                        {
                            while (enumerator.MoveNext())
                            {
                                if (index-- == 0)
                                {
                                    item = enumerator.Current;
                                    return true;
                                }
                            }
                        }

                        return false;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to get an <paramref name="item"/> at the specified <paramref name="index"/> in the <paramref name="collection"/>.
        /// </summary>
        /// <param name="collection">The collection to retrieve the <paramref name="item"/> from.</param>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be returned.</param>
        /// <param name="item">If this method returns <see langword="true"/>, then this parameter contains the found item.</param>
        /// <param name="checkBounds"><see langword="true"/>&#160;to return <see langword="false"/>&#160;if the <paramref name="index"/> is invalid; <see langword="false"/>&#160;to attempt getting the element via the possible interfaces without checking bounds. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <param name="throwError"><see langword="true"/>&#160;to forward any exception thrown by a found getting member; <see langword="false"/>&#160;to suppress the exceptions thrown by the found getting member and return <see langword="false"/>&#160;on failure. This parameter is optional.
        /// <br/>Default value: <see langword="true"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> could be retrieved; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>If <paramref name="collection"/> is neither an <see cref="IList{T}"/> of objects, <see cref="IReadOnlyList{T}"/> of objects, nor an <see cref="IList"/> implementation, then the <paramref name="collection"/> will be iterated.
        /// In this case the <paramref name="checkBounds"/> argument is ignored and the method returns <see langword="false"/>&#160;if the <paramref name="index"/> is invalid.</para>
        /// <note>This method is similar to the <see cref="Enumerable.ElementAtOrDefault{TSource}">Enumerable.ElementAtOrDefault</see> method. The main difference is that if <see cref="Enumerable.ElementAtOrDefault{TSource}">Enumerable.ElementAtOrDefault</see>
        /// returns the default value of the element type, then it cannot be known whether the returned item existed in the collection at the specified position.</note>
        /// </remarks>
        public static bool TryGetElementAt(this IEnumerable collection, int index, out object item, bool checkBounds = true, bool throwError = true)
        {
            if (collection == null)
                Throw.ArgumentNullException(Argument.collection);
            item = default;

            try
            {
                switch (collection)
                {
                    case IList<object> genericList:
                        if (checkBounds && (index < 0 || index >= genericList.Count))
                            return false;
                        item = genericList[index];
                        return true;

#if !(NET35 || NET40)
                    case IReadOnlyList<object> readOnlyList:
                        if (checkBounds && (index < 0 || index >= readOnlyList.Count))
                            return false;
                        item = readOnlyList[index];
                        return true;
#endif

                    case IList list:
                        if (checkBounds && (index < 0 || index > list.Count))
                            return false;
                        item = list[index];
                        return true;
                }
            }
            catch (Exception e) when (!e.IsCriticalOr(throwError))
            {
                return false;
            }

            return collection.Cast<object>().TryGetElementAt(index, out item, checkBounds, throwError);
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
                Throw.ArgumentNullException(Argument.source);
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
                Throw.ArgumentNullException(Argument.random);
            if (source == null)
                Throw.ArgumentNullException(Argument.source);

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
                Throw.ArgumentNullException(Argument.random);
            if (source == null)
                Throw.ArgumentNullException(Argument.source);

            if (source is IList<T> list)
            {
                if (list.Count > 0)
                    return list[random.Next(list.Count)];
            }

#if !NET35 && !NET40
            else if (source is IReadOnlyList<T> readonlyList)
            {
                if (readonlyList.Count > 0)
                    return readonlyList[random.Next(readonlyList.Count)];
            }
#endif
            else
            {
                using (IEnumerator<T> shuffledEnumerator = Shuffle(source, random).GetEnumerator())
                {
                    if (shuffledEnumerator.MoveNext())
                        return shuffledEnumerator.Current;
                }
            }

            if (!defaultIfEmpty)
                Throw.ArgumentException(Argument.source, Res.CollectionEmpty);
            return default;
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

        #region Private Methods

        private static bool IsGenericEnumerableOf([NoEnumeration]this IEnumerable collection, Type genericArgument)
        {
            if (genericEnumerableCache == null)
                Interlocked.CompareExchange(ref genericEnumerableCache, new Cache<Type, Type>(t => Reflector.IEnumerableGenType.GetGenericType(t)).GetThreadSafeAccessor(), null);
            return genericEnumerableCache[genericArgument].IsInstanceOfType(collection);
        }

        /// <summary>
        /// Tries to remove <paramref name="count"/> amount of items from the <paramref name="target"/> at the specified <paramref name="index"/>, and
        /// to insert the specified <paramref name="collection"/> at the same position. The number of elements in <paramref name="collection"/> can be different from the amount of removed items.
        /// This method performs the replace one by one.
        /// </summary>
        private static bool TryReplaceRangeDefault(IEnumerable target, int index, int count, IEnumerable collection, bool checkReadOnlyAndBounds, bool throwError)
        {
            IEnumerator enumerator = collection.GetEnumerator();
            try
            {
                // Copying elements while possible
                int elementsCopied = 0;
                while (count > 0 && enumerator.MoveNext())
                {
                    if (!target.TrySetElementAt(index + elementsCopied++, enumerator.Current, checkReadOnlyAndBounds, throwError))
                        return false;
                    count -= 1;
                }

                // all inserted, removing the rest
                if (count > 0)
                    return target.TryRemoveRange(index + elementsCopied, count, checkReadOnlyAndBounds, throwError);

                // all removed (overwritten), inserting the rest
                IList<object> rest = collection is IList<object> list ? new ListSegment<object>(list, elementsCopied) : enumerator.RestToList();
                if (rest.Count > 0)
                    return target.TryInsertRange(index + elementsCopied, rest, checkReadOnlyAndBounds, throwError);

                // elements to replace had the same size
                return true;
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        #endregion

        #endregion
    }
}
