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
    public static partial class ObjectExtensions
    {
        #region Methods

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the elements of <paramref name="set"/>.
        /// </summary>
        /// <param name="item">The item to search for in <paramref name="set"/>.</param>
        /// <param name="set">The set of items in which to search the specified <paramref name="item"/>.</param>
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
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
        /// <returns><see langword="true"/>&#160;if <paramref name="item"/> is among the results of <paramref name="set"/>; otherwise, <see langword="false"/>.</returns>
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
                    throw new ArgumentException(Res.ArgumentContainsNull, nameof(set));
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
        /// <typeparam name="TTargetType">The desired type of the return value.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An object of <typeparamref name="TTargetType"/>, which is the result of the conversion.</returns>
        /// <exception cref="ArgumentException"><paramref name="obj"/> cannot be converted to <typeparamref name="TTargetType"/>.</exception>
        /// <remarks>
        /// <para>The method firstly tries to use registered direct conversions between source and target types, then attempts to perform the conversion via <see cref="IConvertible"/> types and registered <see cref="TypeConverter"/>s.
        /// If these attempts fail, then the registered conversions tried to be used for intermediate steps, if possible.</para>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <para>A <see cref="TypeConverter"/> can be registered by the <see cref="TypeExtensions.RegisterTypeConverter{TConverter}">RegisterTypeConverter</see>&#160;<see cref="Type"/> extension method.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// <para><typeparamref name="TTargetType"/> can be even a collection type if <paramref name="obj"/> is also an <see cref="IEnumerable"/> implementation.
        /// The target collection type must have either a default constructor or a constructor that can accept a list, array or dictionary as an initializer collection.</para>
        /// </remarks>
        public static TTargetType Convert<TTargetType>(this object obj, CultureInfo culture = null)
            => ObjectConverter.TryConvert(obj, typeof(TTargetType), culture, out object result, out Exception error) && typeof(TTargetType).CanAcceptValue(result)
                ? (TTargetType)result
                : throw new ArgumentException(Res.ObjectExtensionsCannotConvertToType(typeof(TTargetType)), nameof(obj), error);

        /// <summary>
        /// Converts an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <paramref name="targetType"/>.
        /// </summary>
        /// <param name="targetType">The desired type of the return value.</param>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used. This parameter is optional.
        /// <br/>Default value: <see langword="null"/>.</param>
        /// <returns>An object of <paramref name="targetType"/>, which is the result of the conversion.</returns>
        /// <exception cref="ArgumentException"><paramref name="obj"/> cannot be converted to <paramref name="targetType"/>.</exception>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// <para><paramref name="targetType"/> can be even a collection type if <paramref name="obj"/> is also an <see cref="IEnumerable"/> implementation.
        /// The target collection type must have either a default constructor or a constructor that can accept a list, array or dictionary as an initializer collection.</para>
        /// </remarks>
        public static object Convert(this object obj, Type targetType, CultureInfo culture = null)
            => ObjectConverter.TryConvert(obj, targetType, culture, out object result, out Exception error) && targetType.CanAcceptValue(result)
                ? result
                : throw new ArgumentException(Res.ObjectExtensionsCannotConvertToType(targetType), nameof(obj), error);

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <typeparamref name="TTargetType"/>.
        /// </summary>
        /// <typeparam name="TTargetType">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <typeparamref name="TTargetType"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
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

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <typeparamref name="TTargetType"/>.
        /// </summary>
        /// <typeparam name="TTargetType">The desired type of the returned <paramref name="value"/>.</typeparam>
        /// <param name="obj">The object to convert.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <typeparamref name="TTargetType"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static bool TryConvert<TTargetType>(this object obj, out TTargetType value) => TryConvert(obj, null, out value);

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <paramref name="targetType"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="targetType">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <paramref name="targetType"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static bool TryConvert(this object obj, Type targetType, out object value) => TryConvert(obj, targetType, null, out value);

        /// <summary>
        /// Tries to convert an <see cref="object"/> specified in the <paramref name="obj"/> parameter to the desired <paramref name="targetType"/>.
        /// </summary>
        /// <param name="obj">The object to convert.</param>
        /// <param name="targetType">The desired type of the returned <paramref name="value"/>.</param>
        /// <param name="culture">The culture to use for the conversion. If <see langword="null"/>, then the <see cref="CultureInfo.InvariantCulture"/> will be used.</param>
        /// <param name="value">When this method returns with <see langword="true"/>&#160;result, then this parameter contains the result of the conversion.</param>
        /// <returns><see langword="true"/>, if <paramref name="obj"/> could be converted to <paramref name="targetType"/>, which is returned in the <paramref name="value"/> parameter; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// <para>New conversions can be registered by the <see cref="O:KGySoft.CoreLibraries.TypeExtensions.RegisterConversion">RegisterConversion</see>&#160;<see cref="Type"/> extension methods.</para>
        /// <note type="tip">The registered conversions are tried to be used for intermediate conversion steps if possible. For example, if a conversion is registered from <see cref="long"/> to <see cref="IntPtr"/>,
        /// then conversions from other convertible types become automatically available using the <see cref="long"/> type as an intermediate conversion step.</note>
        /// </remarks>
        public static bool TryConvert(this object obj, Type targetType, CultureInfo culture, out object value) => ObjectConverter.TryConvert(obj, targetType, culture, out value, out var _);

        #endregion
    }
}
