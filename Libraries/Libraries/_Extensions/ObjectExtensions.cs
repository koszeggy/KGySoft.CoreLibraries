#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ObjectExtensions.cs
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
using System.Collections.Generic;

using KGySoft.Libraries.Serialization;

#endregion

namespace KGySoft.Libraries
{
    /// <summary>
    /// Extension methods for <see cref="Object"/> class
    /// </summary>
    public static class ObjectExtensions
    {
        #region Methods

        /// <summary>
        /// Gets whether <paramref name="item"/> is among the elements of <paramref name="set"/>.
        /// </summary>
        /// <param name="item">The item to search for in <paramref name="set"/>.</param>
        /// <param name="set">The set of items in which to search the specified <paramref name="item"/>.</param>
        /// <returns><c>true</c> if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method works similarly to the "in" operator in SQL and Pascal.
        /// This overload uses <see cref="object.Equals(object,object)"/> to compare the items.
        /// For better performance use the generic <see cref="In{T}"/> method whenever possible.
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
        /// <returns><c>true</c> if <paramref name="item"/> is among the elements of <paramref name="set"/>; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// This method works similarly to the "in" operator in SQL and Pascal.
        /// This overload uses generic <see cref="IEqualityComparer{T}"/> implementations to compare the items for the best performance.
        /// </remarks>
        public static bool In<T>(this T item, params T[] set)
        {
            int length;
            if (set == null || (length = set.Length) == 0)
                return false;

            IEqualityComparer<T> comparer;
            if (item is Enum) //(typeof(T).IsEnum)
                comparer = EnumComparer<T>.Comparer;
            else
                comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < length; i++)
            {
                if (comparer.Equals(item, set[i]))
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
            BinarySerializationFormatter formatter = new BinarySerializationFormatter();
            byte[] raw = formatter.Serialize(obj);
            return (T)formatter.Deserialize(raw);
        }

        #endregion
    }
}
