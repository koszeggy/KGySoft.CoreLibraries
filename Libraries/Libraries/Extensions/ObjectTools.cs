using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using KGySoft.Libraries.Serialization;

namespace KGySoft.Libraries
{
    /// <summary>
    /// Extension methods for <see cref="Object"/> class
    /// </summary>
    public static class ObjectTools
    {
        /// <summary>
        /// Gets whether <paramref name="item"/> is among the elements of <paramref name="set"/>.
        /// </summary>
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
        /// in such case it is not gueranteed that the result is functionally equivalent to the input object.</remarks>
        public static T DeepClone<T>(this T obj)
        {
            BinarySerializationFormatter formatter = new BinarySerializationFormatter();
            byte[] raw = formatter.Serialize(obj);
            return (T)formatter.Deserialize(raw);
        }
    }
}
