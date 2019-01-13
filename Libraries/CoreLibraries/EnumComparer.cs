#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumComparer.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
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
#if DEBUG
using System.Linq.Expressions;
#endif

using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides an efficient <see cref="IEqualityComparer{T}"/> and <see cref="IComparer{T}"/> implementation for <see cref="Enum"/> types.
    /// Can be used for example in <see cref="Dictionary{TKey,TValue}"/>, <see cref="SortedList{TKey,TValue}"/> or <see cref="Cache{TKey,TValue}"/> instances with <see langword="enum"/>&#160;key,
    /// or as a comparer for <see cref="List{T}.Sort(IComparer{T})"><![CDATA[List<T>.Sort(IComparer<T>)]]></see> method to sort <see langword="enum"/>&#160;elements.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration. Must be an <see cref="Enum"/> type.</typeparam>
    /// <remarks>
    /// Using dictionaries with <see langword="enum"/>&#160;key and finding elements in an <see langword="enum"/>&#160;array works without using <see cref="EnumComparer{TEnum}"/>, too.
    /// But unlike <see cref="int"/> or the other possible underlying types, <see langword="enum"/>&#160;types does not implement the generic <see cref="IEquatable{T}"/> and
    /// <see cref="IComparable{T}"/> interfaces. This causes that using an <see langword="enum"/>&#160;as key in a dictionary, for example, will be very ineffective due to heavy boxing and unboxing to and from
    /// <see cref="object"/> type. This comparer generates the type specific <see cref="IEqualityComparer{T}.Equals(T,T)"><![CDATA[IEqualityComparer<TEnum>.Equals]]></see>,
    /// <see cref="IEqualityComparer{T}.GetHashCode(T)"><![CDATA[IEqualityComparer<T>.GetHashCode]]></see> and <see cref="IComparer{T}.Compare"><![CDATA[IComparer<T>.Compare]]></see> methods for any <see langword="enum"/>&#160;type.
    /// </remarks>
    /// <example>
    /// Example for initializing of a <see cref="Dictionary{TKey,TValue}"/> with <see cref="EnumComparer{TEnum}"/>:
    /// <code lang="C#">
    /// <![CDATA[Dictionary<MyEnum, string> myDict = new Dictionary<MyEnum, string>(EnumComparer<MyEnum>.Comparer);]]>
    /// </code>
    /// </example>
    [Serializable]
    public sealed class EnumComparer<TEnum> : IEqualityComparer<TEnum>, IComparer<TEnum>
    {
        #region Fields

        private static EnumComparer<TEnum> comparer;
#if DEBUG
        private static Func<TEnum, TEnum, bool> equals;
        private static Func<TEnum, int> getHashCode;
        private static Func<TEnum, TEnum, int> compare;
#else
        // ReSharper disable once StaticMemberInGenericType - TEnum specific value
        // ReSharper disable once NotAccessedField.Local - used by RecompILer
        private static bool isUnsignedCompare;
#endif

        #endregion

        #region Properties

        /// <summary>
        /// Gets the comparer instance for <typeparamref name="TEnum"/> type.
        /// </summary>
        public static EnumComparer<TEnum> Comparer => comparer ?? (comparer = new EnumComparer<TEnum>());

        #endregion

        #region Constructors

        /// <summary>
        /// Private constructor to prevent direct instantiation.
        /// </summary>
        private EnumComparer()
        {
            // this could be in static ctor but that would throw a TypeInitializationException at unexpected place
            if (!typeof(TEnum).IsEnum)
                throw new InvalidOperationException(Res.EnumTypeParameterInvalid);

#if !DEBUG
            // this could be in static ctor but will be set only once per type when Comparer is accessed.
            isUnsignedCompare = Enum.GetUnderlyingType(typeof(TEnum)) == typeof(ulong);
#endif
        }

        #endregion

        #region Methods

        #region Static Methods

#if DEBUG
        private static Func<TEnum, TEnum, bool> GenerateEquals()
        {
            // Cannot use x == y because compiler says that operator "==" cannot applied between TEnum and TEnum.
            // But in a generated code such equality check will not be a problem.

            ParameterExpression xParameter = Expression.Parameter(typeof(TEnum), "x");
            ParameterExpression yParameter = Expression.Parameter(typeof(TEnum), "y");
            BinaryExpression equalExpression = Expression.Equal(xParameter, yParameter);

            return Expression.Lambda<Func<TEnum, TEnum, bool>>(equalExpression, xParameter, yParameter).Compile();
        }

        private static Func<TEnum, int> GenerateGetHashCode()
        {
            // Original GetHashCode is extremely slow on enums because they retrieve internal value as object first.
            // But casting self value to the underlying type and calling GetHashCode on that value returns the same hash code much more fast.

            ParameterExpression objParameter = Expression.Parameter(typeof(TEnum), "obj");
            Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            UnaryExpression enumCastedToUnderlyingType = Expression.Convert(objParameter, underlyingType);
            // ReSharper disable once AssignNullToNotNullAttribute - the constructor ensures TEnum has an underlying enum type
            MethodCallExpression getHashCodeCall = Expression.Call(enumCastedToUnderlyingType, underlyingType.GetMethod(nameof(Object.GetHashCode)));

            return Expression.Lambda<Func<TEnum, int>>(getHashCodeCall, objParameter).Compile();
        }

        private static Func<TEnum, TEnum, int> GenerateCompare()
        {
            // Original Enum implements only non-generic IComparable with an extremely slow CompareTo
            // This implementation calls CompareTo on underlying type: x.CompareTo(y)

            Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            ParameterExpression xParameter = Expression.Parameter(typeof(TEnum), "x");
            ParameterExpression yParameter = Expression.Parameter(typeof(TEnum), "y");
            UnaryExpression xAsUnderlyingType = Expression.Convert(xParameter, underlyingType);
            UnaryExpression yAsUnderlyingType = Expression.Convert(yParameter, underlyingType);
            // ReSharper disable once AssignNullToNotNullAttribute - the constructor ensures TEnum has is a real enum with a comparable underlying type
            MethodCallExpression compareToCall = Expression.Call(xAsUnderlyingType, underlyingType.GetMethod(nameof(IComparable<_>.CompareTo), new Type[] { underlyingType }), yAsUnderlyingType);

            return Expression.Lambda<Func<TEnum, TEnum, int>>(compareToCall, xParameter, yParameter).Compile();
        }
#endif

        #endregion

        #region Instance Methods

        /// <summary>
        /// Determines whether the specified <paramref name="obj"/> is the same type of <see cref="EnumComparer{TEnum}"/> as the current instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><see langword="true"/>&#160;if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object obj) => obj is EnumComparer<TEnum>;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() => GetType().GetHashCode();

        #endregion

        #endregion

        #region IEqualityComparer<TEnum> Members

        /// <summary>
        /// Determines whether two <typeparamref name="TEnum"/> instances are equal.
        /// </summary>
        /// <returns>
        /// <see langword="true"/>&#160;if the specified values are equal; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="x">The first <typeparamref name="TEnum"/> value to compare.</param>
        /// <param name="y">The second <typeparamref name="TEnum"/> value to compare.</param>
        public bool Equals(TEnum x, TEnum y)
        {
#if DEBUG
            if (equals == null)
                equals = GenerateEquals();
            return equals(x, y);
#else
            // replaced to the next code by RecompILer:
            // return ((long)x).Equals((long)y);

            return false;
#endif
        }

        /// <summary>
        /// Returns a hash code for the specified <typeparamref name="TEnum"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for the specified <typeparamref name="TEnum"/> instance.
        /// </returns>
        /// <param name="obj">The <typeparamref name="TEnum"/> for which a hash code is to be returned.</param>
        /// <remarks>Returned hash code is not necessarily equals with own hash code of an <see langword="enum"/>&#160;value but provides a fast and well-spread value.</remarks>
        public int GetHashCode(TEnum obj)
        {
#if DEBUG
            if (getHashCode == null)
                getHashCode = GenerateGetHashCode();
            return getHashCode(obj);
#else
            // replaced to the next code by RecompILer:
            // return ((int)obj).GetHashCode();

            return 0;
#endif
        }

        #endregion

        #region IComparer<TEnum> Members

        /// <summary>
        /// Compares two <typeparamref name="TEnum"/> instances and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <returns>
        /// <list type="table">
        /// <listheader><term>Value</term>&#160;<description>Condition</description></listheader>
        /// <item><term>Less than zero</term>&#160;<description><paramref name="x"/> is less than <paramref name="y"/>.</description></item>
        /// <item><term>Zero</term>&#160;<description><paramref name="x"/> equals <paramref name="y"/>.</description></item>
        /// <item><term>Greater than zero</term>&#160;<description><paramref name="x"/> is greater than <paramref name="y"/>.</description></item>
        /// </list>
        /// </returns>
        /// <param name="x">The first <typeparamref name="TEnum"/> instance to compare.</param>
        /// <param name="y">The second <typeparamref name="TEnum"/> instance to compare.</param>
        public int Compare(TEnum x, TEnum y)
        {
#if DEBUG
            if (compare == null)
                compare = GenerateCompare();
            return compare(x, y);
#else
            // replaced to the next code by RecompILer:
            // return isUnsignedCompare ? ((ulong)x).CompareTo((ulong)y) : ((long)x).CompareTo((long)y);

            return 0;
#endif
        }

        #endregion
    }
}
