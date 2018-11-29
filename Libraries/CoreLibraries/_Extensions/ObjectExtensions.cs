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

        public static TTargetType Convert<TTargetType>(this object obj, CultureInfo culture = null)
            => TryConvert(obj, typeof(TTargetType), culture, out object result, out Exception error) && typeof(TTargetType).CanAcceptValue(result)
                ? (TTargetType)result
                : throw new ArgumentException(Res.Get(Res.CannotConvertToType, typeof(TTargetType)), nameof(obj), error);

        public static bool TryConvert<TTargetType>(this object obj, CultureInfo culture, out TTargetType value)
        {
            bool success = TryConvert(obj, typeof(TTargetType), culture, out object result);
            if (success && typeof(TTargetType).CanAcceptValue(result))
                value = (TTargetType)result;
            else
                throw new ArgumentException(Res.Get(Res.CannotConvertToType, typeof(TTargetType)), nameof(obj));
            return true;
        }

        public static bool TryConvert<TTargetType>(this object obj, out TTargetType value) => TryConvert(obj, null, out value);

        public static bool TryConvert(this object obj, Type targetType, out object value) => TryConvert(obj, targetType, null, out value);

        public static bool TryConvert(this object obj, Type targetType, CultureInfo culture, out object value) => TryConvert(obj, targetType, culture, out value, out var _);

        public static bool TryConvert(this object obj, Type targetType, CultureInfo culture, out object value, out Exception error)
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

            Type sourceType = obj.GetType();
            // ReSharper disable once PossibleNullReferenceException
            if (targetType.IsAssignableFrom(sourceType))
            {
                value = obj;
                return true;
            }

            // trying parse from string...
            return obj is string strValue && TryParseFromString(targetType, strValue, culture, out value, ref error)
                // ...IConvertible...
                || obj is IConvertible convertible && typeof(IConvertible).IsAssignableFrom(targetType) && TryConvertCovertible(convertible, targetType, culture, out value, ref error)
                // ...and TypeCovnerter
                || TryConvertByConverter(obj, targetType, culture, out value, ref error);
        }

        private static bool TryParseFromString(Type targetType, string value, CultureInfo culture, out object result, ref Exception error)
        {
            if (!Reflector.CanParseNatively(targetType))
            {
                result = null;
                return false;
            }

            try
            {
                result = Reflector.Parse(targetType, value, null, culture);
                return true;
            }
            catch (Exception e)
            {
                error = e;
                result = null;
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

        private static bool TryConvertByConverter(object source, Type targetType, CultureInfo culture, out object value, ref Exception error)
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

        #endregion
    }
}
