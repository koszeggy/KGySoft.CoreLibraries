#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectExtensions.cs
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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using KGySoft.Reflection;
using KGySoft.Serialization;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Contains extension methods for the <see cref="Object"/> type.
    /// </summary>
    public static class ObjectExtensions
    {
        #region Methods

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the elements of <paramref name="set"/>.
        /// </summary>
        /// <param name="item">The item to search for in <paramref name="set"/>.</param>
        /// <param name="set">The set of items in which to search the specified <paramref name="item"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method works similarly to the <c>in</c> operator in SQL and Pascal.</para>
        /// <para>This overload uses <see cref="object.Equals(object,object)">Object.Equals</see> method to compare the items.
        /// <note>For better performance use the generic <see cref="In{T}(T,T[])"/> or <see cref="In{T}(T,Func{T}[])"/> methods whenever possible.</note></para>
        /// </remarks>
        public static bool In(this object item, params object[] set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            for (int i = 0; i < length; i++)
            {
                if (Equals(item, set[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the elements of <paramref name="set"/>.
        /// </summary>
        /// <param name="item">The item to search for in <paramref name="set"/>.</param>
        /// <param name="set">The set of items in which to search the specified <paramref name="item"/>.</param>
        /// <typeparam name="T">The type of <paramref name="item"/> and the <paramref name="set"/> elements.</typeparam>
        /// <returns><see langword="true"/> if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method works similarly to the <c>in</c> operator in SQL and Pascal.</para>
        /// <para>This overload uses generic <see cref="IEqualityComparer{T}"/> implementations to compare the items for the best performance.
        /// <note>If elements of <paramref name="set"/> are complex expressions consider to use the <see cref="In{T}(T,Func{T}[])"/> overload instead to prevent evaluating all elements until they are actually compared.</note></para>
        /// </remarks>
        public static bool In<T>(this T item, params T[] set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            var comparer = item is Enum ? (IEqualityComparer<T>)EnumComparer<T>.Comparer : EqualityComparer<T>.Default;
            for (int i = 0; i < length; i++)
            {
                if (comparer.Equals(item, set[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the results of <paramref name="set"/>.
        /// </summary>
        /// <param name="item">The item to search for in the results of <paramref name="set"/>.</param>
        /// <param name="set">The set of delegates, whose results are checked whether they are equal to the specified <paramref name="item"/>.</param>
        /// <typeparam name="T">The type of <paramref name="item"/> and the <paramref name="set"/> elements.</typeparam>
        /// <returns><see langword="true"/> if <paramref name="item"/> is among the results of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>This method works similarly to the <c>in</c> operator in SQL and Pascal.</para>
        /// <para>This overload uses generic <see cref="IEqualityComparer{T}"/> implementations to compare the items for the best performance.
        /// The elements of <paramref name="set"/> are evaluated only when they are actually compared so if a result is found the rest of the elements will not be evaluated.
        /// <note>If elements of <paramref name="set"/> are constants or simple expressions consider to use the <see cref="In{T}(T,T[])"/> overload to eliminate the overhead of delegate invokes.</note></para>
        /// </remarks>
        public static bool In<T>(this T item, params Func<T>[] set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            var comparer = item is Enum ? (IEqualityComparer<T>)EnumComparer<T>.Comparer : EqualityComparer<T>.Default;
            for (int i = 0; i < length; i++)
            {
                Func<T> func = set[i];
                if (func == null)
                    throw new ArgumentException(Res.Get(Res.ArgumentContainsNull), nameof(set));
                if (comparer.Equals(item, func.Invoke()))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Clones an object by deep cloning.
        /// </summary>
        /// <typeparam name="T">Type of the object</typeparam>
        /// <param name="obj">The object to clone.</param>
        /// <returns>The functionally equivalent clone of the object.</returns>
        /// <remarks>This method clones types even without <see cref="SerializableAttribute"/>; however,
        /// in such case it is not guaranteed that the result is functionally equivalent to the input object.</remarks>
        public static T DeepClone<T>(this T obj)
        {
            var formatter = new BinarySerializationFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.SerializeToStream(stream, obj);
                stream.Position = 0L;
                return (T)formatter.DeserializeFromStream(stream);
            }
        }

        /// <summary>
        /// Converts an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <typeparamref name="TTargetType"/>.
        /// </summary>
        /// <typeparam name="TTargetType">The type of the desired return value.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An object of <typeparamref name="TTargetType"/>, which is the result of the conversion.</returns>
        /// <exception cref="ArgumentException"><paramref name="obj"/> cannot be converted to <typeparamref name="TTargetType"/>.</exception>
        /// <remarks>
        /// <para>The method firstly tries to use registered direct conversions between source and target types, then attempts to perform the conversion via <see cref="IConvertible"/> types and registered <see cref="TypeConverter"/>s.
        /// If these attempts fail, then the registered conversions tried to be used for intermediate steps, if possible.</para>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see> <see cref="Type"/> extension methods.</para>
        /// <para>New <see cref="TypeConverter"/> instances can be registered by the <see cref="TypeExtensions.RegisterTypeConverter{TConverter}">RegisterTypeConverter</see> <see cref="Type"/> extension method.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static TTargetType Convert<TTargetType>(this object obj, CultureInfo culture = null)
            => TryConvert(obj, typeof(TTargetType), culture, out object result, out Exception error) && typeof(TTargetType).CanAcceptValue(result)
                ? (TTargetType)result
                : throw new ArgumentException(Res.ObjectExtensionsCannotConvertToType(typeof(TTargetType)), nameof(obj), error);

        /// <summary>
        /// Converts an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType">The type of the desired return value.</param>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An object of <paramref name="targetType"/>, which is the result of the conversion.</returns>
        /// <exception cref="ArgumentException"><paramref name="obj"/> cannot be converted to <paramref name="targetType"/>.</exception>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see> <see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static object Convert(this object obj, Type targetType, CultureInfo culture = null)
            => TryConvert(obj, targetType, culture, out object result, out Exception error) && targetType.CanAcceptValue(result)
                ? result
                : throw new ArgumentException(Res.ObjectExtensionsCannotConvertToType(targetType), nameof(obj), error);

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <typeparamref name="TTargetType"/>.
        /// </summary>
        /// <typeparam name="TTargetType">The type of the desired return value.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/> result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <typeparamref name="TTargetType"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see> <see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static bool TryConvert<TTargetType>(this object obj, CultureInfo culture, out TTargetType value)
        {
            if (TryConvert(obj, typeof(TTargetType), culture, out object result) && typeof(TTargetType).CanAcceptValue(result))
            {
                value = (TTargetType)result;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryConvert<TTargetType>(this object obj, out TTargetType value) => TryConvert(obj, null, out value);

        public static bool TryConvert(this object obj, Type targetType, out object value) => TryConvert(obj, targetType, null, out value);

        public static bool TryConvert(this object obj, Type targetType, CultureInfo culture, out object value) => TryConvert(obj, targetType, culture, out value, out var _);

        private static bool TryConvert(object obj, Type targetType, CultureInfo culture, out object value, out Exception error)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType), Res.ArgumentNull);

            error = null;
            if (obj == null || obj is DBNull)
            {
                value = null;
                return targetType.CanAcceptValue(null);
            }

            if (targetType.IsNullable())
                targetType = Nullable.GetUnderlyingType(targetType);

            // ReSharper disable once PossibleNullReferenceException
            if (targetType.IsInstanceOfType(obj))
            {
                value = obj;
                return true;
            }

            if (culture == null)
                culture = CultureInfo.InvariantCulture;

            return DoConvert(obj, targetType, culture, out value, ref error, null);
        }

        private static bool DoConvert(object obj, Type targetType, CultureInfo culture, out object value, ref Exception error, HashSet<(object Instance, Type SourceType, Type TargetType)> failedAttempts)
        {
            Type sourceType = obj.GetType();
            if (failedAttempts?.Contains((obj, sourceType, targetType)) == true)
            {
                value = null;
                return false;
            }

            // direct conversions between the source and target types are used in the first place
            bool result = sourceType.GetConversion(targetType) is Delegate conversion && TryConvertByRegisteredCovnersion(obj, conversion, targetType, culture, out value, ref error)
                // if it fails, then trying to parse from string...
                || obj is string strValue && strValue.TryParse(targetType, culture, out value)
                // ...IConvertible...
                || obj is IConvertible convertible && typeof(IConvertible).IsAssignableFrom(targetType) && TryConvertCovertible(convertible, targetType, culture, out value, ref error)
                // ...and TypeCovnerter
                || TryConvertByTypeConverter(obj, targetType, culture, out value, ref error);

            if (result)
                return true;

            if (failedAttempts == null)
                failedAttempts = new HashSet<(object, Type, Type)>();

            // if both source and target types are enumerable, trying to convert their types, too
            if (obj is IEnumerable collection && Reflector.IEnumerableType.IsAssignableFrom(targetType) && TryConvertCollection(collection, targetType, culture, out value, ref error, failedAttempts))
                return true;

            failedAttempts.Add((obj, sourceType, targetType));

            // if there are registered converters to the target type, then we try to convert the value for those
            Type[] sourceTypes = targetType.GetConversionSourceTypes();
            if (sourceTypes.Length == 0)
                return false;

            foreach (Type intermediateType in sourceTypes)
            {
                if (DoConvert(obj, intermediateType, culture, out object intermediateResult, ref error, failedAttempts) && DoConvert(intermediateResult, targetType, culture, out value, ref error, failedAttempts))
                    return true;
            }

            return false;
        }

        private static bool TryConvertByRegisteredCovnersion(object obj, Delegate conversionDelegate, Type targetType, CultureInfo culture, out object value, ref Exception error)
        {
            value = null;
            try
            {
                switch (conversionDelegate)
                {
                    case ConversionAttempt conversionAttempt:
                        return conversionAttempt.Invoke(obj, culture, out value) && targetType.CanAcceptValue(value);
                    case Conversion conversion:
                        value = conversion.Invoke(obj, culture);
                        return targetType.CanAcceptValue(value);
                    default:
                        throw new InvalidOperationException("Invalid conversion delegate type");
                }
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
        }

        private static bool TryConvertCovertible(IConvertible convertible, Type targetType, CultureInfo culture, out object value, ref Exception error)
        {
            try
            {
                if (targetType.IsEnum)
                {
                    value = Enum.ToObject(targetType, convertible);
                    return true;
                }

                value = convertible.ToType(targetType, culture);
                return true;

            }
            catch (Exception e)
            {
                error = e;
                value = null;
                return false;
            }
        }

        private static bool TryConvertByTypeConverter(object source, Type targetType, CultureInfo culture, out object value, ref Exception error)
        {
            value = null;
            Type sourceType = source.GetType();

            // 1.) by target
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(sourceType))
            {
                try
                {
                    // ReSharper disable once AssignNullToNotNullAttribute - actually it CAN be null...
                    value = converter.ConvertFrom(null, culture, source);
                    return true;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            // 2.) by source
            converter = TypeDescriptor.GetConverter(sourceType);
            if (converter.CanConvertTo(targetType))
            {
                try
                {
                    value = converter.ConvertTo(null, culture, source, targetType);
                }
                catch (Exception e)
                {
                    error = e;
                    return false;
                }
            }

            return false;
        }

        private static bool TryConvertCollection(IEnumerable collection, Type targetType, CultureInfo culture, out object value, ref Exception error, HashSet<(object, Type, Type)> failedAttempts)
        {
            if (targetType.IsArray)
                return TryConvertToArray(collection, targetType, culture, out value, ref error, failedAttempts);
        }

        private static bool TryConvertToArray(IEnumerable sourceCollection, Type targetType, CultureInfo culture, out object value, ref Exception error, HashSet<(object, Type, Type)> failedAttempts)
        {
            value = null;
            Type sourceType = sourceCollection.GetType();
            int rank = targetType.GetArrayRank();
            Type targetElementType = targetType.GetElementType();
            Array targetArray;

            // multi dimension target array is supported only if the source is also an array and has the same dimension
            if (rank > 1)
            {
                if (!(sourceCollection is Array sourceArray) || sourceArray.Rank != rank)
                    return false;

                int[] lengths = new int[rank];
                int[] lowerBounds = new int[rank];
                for (int i = 0; i < rank; i++)
                {
                    lengths[i] = sourceArray.GetLength(i);
                    lowerBounds[i] = sourceArray.GetLowerBound(i);
                }

                // ReSharper disable once AssignNullToNotNullAttribute - sourceType is an array here
                targetArray = Array.CreateInstance(sourceType.GetElementType(), lengths, lowerBounds);
                var indexer = new ArrayIndexer(lengths, lowerBounds);
                foreach (object sourceItem in sourceArray)
                {
                    indexer.MoveNext();
                    if (!DoConvert(sourceItem, targetElementType, culture, out object targetItem, ref error, failedAttempts))
                        return false;
                    targetArray.SetValue(targetItem, indexer.Current);
                }

                value = targetArray;
                return true;
            }

            // single dimension target array below - case 1: source size is known
            if (sourceCollection is ICollection collection)
            {
                // ReSharper disable once AssignNullToNotNullAttribute - target is array in this method
                targetArray = Array.CreateInstance(targetElementType, collection.Count);
                int i = 0;
                foreach (object sourceItem in collection)
                {
                    if (!DoConvert(sourceItem, targetElementType, culture, out object targetItem, ref error, failedAttempts))
                        return false;
                    targetArray.SetValue(targetItem, i++);
                }

                value = targetArray;
                return true;
            }

            // case 2: source size is not known: using a List
            IList resultList = (IList)Reflector.CreateInstance(Reflector.ListGenType.MakeGenericType(targetElementType));
            foreach (object sourceItem in sourceCollection)
            {
                if (!DoConvert(sourceItem, targetElementType, culture, out object targetItem, ref error, failedAttempts))
                    return false;
                resultList.Add(targetItem);
            }

            // ReSharper disable once AssignNullToNotNullAttribute - target is array in this method
            targetArray = Array.CreateInstance(targetElementType, resultList.Count);
            resultList.CopyTo(targetArray, 0);
            value = targetArray;
            return true;
        }

        #endregion
    }
}
