#region Used namespaces

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using KGySoft.Collections;

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Efficient <see cref="IEqualityComparer{T}"/> and <see cref="IComparer{T}"/> implementation for <see cref="Enum"/> types.
    /// Can be used for example in <see cref="Dictionary{TKey,TValue}"/>, <see cref="SortedList{TKey,TValue}"/> or <see cref="Cache{TKey,TValue}"/> instances with enum key,
    /// or as a comparer for <see cref="List{T}.Sort(System.Collections.Generic.IComparer{T})"/> method to sort enum elements.
    /// </summary>
    /// <typeparam name="TEnum">Enum type.</typeparam>
    /// <remarks>
    /// Using dictionaries with enum key and finding elements in an enum array works without using <see cref="EnumComparer{TEnum}"/>, too.
    /// But unlike <see cref="int"/> or the other possible underlying types, enum types does not implement the generic <see cref="IEquatable{T}"/> and
    /// <see cref="IComparable{T}"/> interfaces. This causes that using an enum as key in a dictionary, for example, will be very ineffective due to heavy boxing and unboxing to and from
    /// <see cref="object"/> type. This comparer generates the type specific <c>Equals</c>, <c>GetHashCode</c> and <c>CompareTo</c> methods foy any enum type.
    /// </remarks>
    /// <example>
    /// Example for initializing of a <see cref="Dictionary{TKey,TValue}"/> with <see cref="EnumComparer{TEnum}"/>:
    /// <code lang="C#">
    /// Dictionary&lt;MyEnum, string&gt; myDict = new Dictionary&lt;MyEnum, string&gt;(EnumComparer&lt;MyEnum&gt;.Comparer);
    /// </code>
    /// </example>
    [Serializable]
    public sealed class EnumComparer<TEnum>: IEqualityComparer<TEnum>, IComparer<TEnum>
    {
        #region Fields

        private static EnumComparer<TEnum> comparer;
#if DEBUG
        private static Func<TEnum, TEnum, bool> equals;
        private static Func<TEnum, int> getHashCode;
        private static Func<TEnum, TEnum, int> compare;
#else
        private static bool isUnsignedCompare;
#endif

        #endregion

        #region Properties

        /// <summary>
        /// Gets the comparer instance for <typeparamref name="TEnum"/> type.
        /// </summary>
        public static EnumComparer<TEnum> Comparer
        {
            get { return comparer ?? (comparer = new EnumComparer<TEnum>()); }
        }

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
            MethodCallExpression getHashCodeCall = Expression.Call(enumCastedToUnderlyingType, underlyingType.GetMethod("GetHashCode"));

            return Expression.Lambda<Func<TEnum, int>>(getHashCodeCall, objParameter).Compile();
        }

        private static Func<TEnum, TEnum, int> GenerateCompare()
        {
            // Original Enum implements only non-generic IComparable with an extremely slow CompareTo
            // This implementation calls CompareTo on underlying type: x.CompareTo(y)

            Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            ParameterExpression xParameter = Expression.Parameter(typeof(TEnum), "x");
            ParameterExpression yParameter = Expression.Parameter(typeof(TEnum), "y");
            UnaryExpression xCastedToUnderlyingType = Expression.Convert(xParameter, underlyingType);
            UnaryExpression yCastedToUnderlyingType = Expression.Convert(yParameter, underlyingType);
            MethodCallExpression compareToCall = Expression.Call(xCastedToUnderlyingType, underlyingType.GetMethod("CompareTo", new Type[] { underlyingType }), yCastedToUnderlyingType);

            return Expression.Lambda<Func<TEnum, TEnum, int>>(compareToCall, xParameter, yParameter).Compile();
        }
#endif

        #endregion

        #region Instance Methods

        /// <summary>
        /// Determines whether the specified <paramref name="obj"/> is the same type of <see cref="EnumComparer{TEnum}"/> as the current instance.
        /// </summary>
        public override bool Equals(object obj)
        {
            EnumComparer<TEnum> other = obj as EnumComparer<TEnum>;
            return (other != null);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="EnumComparer{TEnum}"/>.
        /// </returns>
        public override int GetHashCode()
        {
            return GetType().FullName.GetHashCode();
        }

        #endregion

        #endregion

        #region IEqualityComparer<TEnum> Members

        /// <summary>
        /// Determines whether two <typeparamref name="TEnum"/> instances are equal.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the specified enums are equal; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="x">The first enum of type <typeparamref name="TEnum"/> to compare.</param>
        /// <param name="y">The second enum of type <typeparamref name="TEnum"/> to compare.</param>
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
        /// <remarks>Returned hash code is not neccessarily equals with own hash code of an anum value but provides a fast and well-spread value.</remarks>
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
        /// <listheader><term>Value</term> <description>Condition</description></listheader>
        /// <item><term>Less than zero</term> <description><paramref name="x"/> is less than <paramref name="y"/>.</description></item>
        /// <item><term>Zero</term> <description><paramref name="x"/> equals <paramref name="y"/>.</description></item>
        /// <item><term>Greater than zero</term> <description><paramref name="x"/> is greater than <paramref name="y"/>.</description></item>
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
            // return unsignedCompare ? ((ulong)x).CompareTo((ulong)y) : ((long)x).CompareTo((long)y);

            return 0;
#endif
        }

        #endregion
    }
}
