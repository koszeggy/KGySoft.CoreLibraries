#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: EnumComparer.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
#if !NET35
using System.Linq.Expressions; 
#endif
using System.Runtime.Serialization;
using System.Security;
#if !NET35
using System.Threading; 
#endif

using KGySoft.Collections;
#if !NET35
using KGySoft.Reflection;
#endif

#endregion

#region Suppressions

#if NETCOREAPP3_0_OR_GREATER
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes). - Equals/Compare parameters are never null in this class but the constraint is checked in ctor  
#endif

#endregion

namespace KGySoft.CoreLibraries
{
    /// <summary>
    /// Provides an efficient <see cref="IEqualityComparer{T}"/> and <see cref="IComparer{T}"/> implementation for <see cref="Enum"/> types.
    /// Can be used for example in <see cref="Dictionary{TKey,TValue}"/>, <see cref="SortedList{TKey,TValue}"/> or <see cref="Cache{TKey,TValue}"/> instances with <see langword="enum"/> key,
    /// or as a comparer for <see cref="List{T}.Sort(IComparer{T})"><![CDATA[List<T>.Sort(IComparer<T>)]]></see> method to sort <see langword="enum"/> elements.
    /// </summary>
    /// <typeparam name="TEnum">The type of the enumeration. Must be an <see cref="Enum"/> type.</typeparam>
    /// <remarks>
    /// Using dictionaries with <see langword="enum"/> key and finding elements in an <see langword="enum"/> array works without using <see cref="EnumComparer{TEnum}"/>, too.
    /// But unlike <see cref="int"/> or the other possible underlying types, <see langword="enum"/> types does not implement the generic <see cref="IEquatable{T}"/> and
    /// <see cref="IComparable{T}"/> interfaces. This causes that using an <see langword="enum"/> as key in a dictionary, for example, can be very ineffective (depends on the used framework, see the note below)
    /// due to heavy boxing and unboxing to and from <see cref="object"/> type. This comparer generates the type specific <see cref="IEqualityComparer{T}.Equals(T,T)"><![CDATA[IEqualityComparer<TEnum>.Equals]]></see>,
    /// <see cref="IEqualityComparer{T}.GetHashCode(T)"><![CDATA[IEqualityComparer<T>.GetHashCode]]></see> and <see cref="IComparer{T}.Compare"><![CDATA[IComparer<T>.Compare]]></see> methods for any <see langword="enum"/> type.
    /// <note>
    /// The optimization of <see cref="EqualityComparer{T}"/> and <see cref="Comparer{T}"/> instances for <see langword="enum"/> types may differ in different target frameworks.
    /// <list type="bullet">
    /// <item>In .NET Framework 3.5 and earlier versions they are not optimized at all.</item>
    /// <item>In .NET 4.0 Framework <see cref="EqualityComparer{T}"/> was optimized for <see cref="int"/>-based <see langword="enum"/>s. (Every .NET 4.0 assembly is executed on the latest 4.x runtime though, so this might be relevant
    /// only on Windows XP where no newer than the 4.0 runtime can be installed.)</item>
    /// <item>In latest .NET 4.x Framework versions <see cref="EqualityComparer{T}"/> is optimized for any <see langword="enum"/> type but <see cref="Comparer{T}"/> is not.</item>
    /// <item>In .NET Core both <see cref="EqualityComparer{T}"/> and <see cref="Comparer{T}"/> are optimized for any <see langword="enum"/> types
    /// so the performance benefit of using <see cref="EnumComparer{TEnum}"/> in .NET Core is negligible.</item>
    /// </list>
    /// </note>
    /// <note>In .NET Standard 2.0 building dynamic assembly is not supported so the .NET Standard 2.0 version falls back to use <see cref="EqualityComparer{T}"/> and <see cref="Comparer{T}"/> classes.</note>
    /// </remarks>
    /// <example>
    /// Example for initializing of a <see cref="Dictionary{TKey,TValue}"/> with <see cref="EnumComparer{TEnum}"/>:
    /// <code lang="C#">
    /// <![CDATA[Dictionary<MyEnum, string> myDict = new Dictionary<MyEnum, string>(EnumComparer<MyEnum>.Comparer);]]>
    /// </code>
    /// </example>
    [Serializable]
    [CLSCompliant(false)]
#if NET5_0_OR_GREATER
    [SuppressMessage("Usage", "CA2229:Implement serialization constructors", Justification = "False alarm, SerializationUnityHolder will be deserialized.")]
#endif
    public abstract class EnumComparer<TEnum> : IEqualityComparer<TEnum>, IComparer<TEnum>, ISerializable
    {
        #region Nested Classes

        #region SerializationUnityHolder class

        /// <summary>
        /// This class is needed in order not to serialize the generated type.
        /// </summary>
        [Serializable]
        private sealed class SerializationUnityHolder : IObjectReference
        {
            #region Methods

            [SecurityCritical]
            public object GetRealObject(StreamingContext context) => Comparer;

            #endregion
        }

        #endregion

        #region FallbackEnumComparer
#if !NET35

        /// <summary>
        /// A fallback comparer that uses the standard <see cref="EqualityComparer{T}"/> and <see cref="Comparer{T}"/>
        /// classes for comparisons and dynamic delegates for conversions.
        /// This class can be used from .NET Standard 2.0 and partially trusted domains.
        /// </summary>
        [Serializable]
        private sealed class FallbackEnumComparer : EnumComparer<TEnum>
        {
            #region Fields

            private Func<ulong, TEnum>? toEnum;
            private Func<TEnum, ulong>? toUInt64;
            private Func<TEnum, long>? toInt64;

            #endregion

            #region Methods

            #region Static Methods

            /// <summary>
            /// return (TEnum)value;
            /// </summary>
            private static Func<ulong, TEnum> GenerateToEnum()
            {
                ParameterExpression valueParamExpression = Expression.Parameter(Reflector.ULongType, "value");
                return Expression.Lambda<Func<ulong, TEnum>>(Expression.Convert(valueParamExpression, typeof(TEnum)), valueParamExpression).Compile();
            }

            /// <summary><![CDATA[
            /// return (ulong)value & sizeMask;
            /// ]]></summary>
            private static Func<TEnum, ulong> GenerateToUInt64()
            {
                ParameterExpression valueParamExpression = Expression.Parameter(typeof(TEnum), "value");

                // unsigned types simply returning converted value
                if (!typeof(TEnum).IsSignedIntegerType())
                    return Expression.Lambda<Func<TEnum, ulong>>(Expression.Convert(valueParamExpression, Reflector.ULongType), valueParamExpression).Compile();

                var asULong = Expression.Convert(valueParamExpression, Reflector.ULongType);
                var applyMask = Expression.And(asULong, Expression.Constant(typeof(TEnum).GetSizeMask()));
                return Expression.Lambda<Func<TEnum, ulong>>(applyMask, valueParamExpression).Compile();
            }

            /// <summary>
            /// return (long)value;
            /// </summary>
            private static Func<TEnum, long> GenerateToInt64()
            {
                ParameterExpression valueParamExpression = Expression.Parameter(typeof(TEnum), "value");
                return Expression.Lambda<Func<TEnum, long>>(Expression.Convert(valueParamExpression, Reflector.LongType), valueParamExpression).Compile();
            }

            #endregion

            #region Instance Methods

            #region Public Methods

            public override bool Equals(TEnum x, TEnum y) => EqualityComparer<TEnum>.Default.Equals(x, y);
            public override int GetHashCode(TEnum obj) => EqualityComparer<TEnum>.Default.GetHashCode(obj!);
            public override int Compare(TEnum x, TEnum y) => Comparer<TEnum>.Default.Compare(x, y);

            #endregion

            #region Protected-Internal Methods

            protected internal override TEnum ToEnum(ulong value)
            {
                if (toEnum == null)
                    Interlocked.CompareExchange(ref toEnum, GenerateToEnum(), null);
                return toEnum.Invoke(value);
            }

            protected internal override ulong ToUInt64(TEnum value)
            {
                if (toUInt64 == null)
                    Interlocked.CompareExchange(ref toUInt64, GenerateToUInt64(), null);
                return toUInt64.Invoke(value);
            }

            protected internal override long ToInt64(TEnum value)
            {
                if (toInt64 == null)
                    Interlocked.CompareExchange(ref toInt64, GenerateToInt64(), null);
                return toInt64.Invoke(value);
            }

            #endregion

            #endregion

            #endregion
        }

#endif
        #endregion

        #endregion

        #region Fields

        private static EnumComparer<TEnum>? comparer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the comparer instance for <typeparamref name="TEnum"/> type.
        /// </summary>
        public static EnumComparer<TEnum> Comparer => comparer ??=
#if NET35
            EnumComparerBuilder.GetComparer<TEnum>();
#elif NETFRAMEWORK
            EnvironmentHelper.IsPartiallyTrustedDomain
                ? new FallbackEnumComparer()
                : EnumComparerBuilder.GetComparer<TEnum>();
#elif NETSTANDARD2_0
            new FallbackEnumComparer();
#else
            // NOTE: TypeBuilder.CreateType may throw a BadImageFormat exception on Android, .NET 6.
            //       And though it works well on Linux (or even on Android with the .NET Standard 2.1 build) not risking using it on non-Windows platforms
            EnvironmentHelper.IsWindows
                ? EnumComparerBuilder.GetComparer<TEnum>()
                : new FallbackEnumComparer();
#endif

        #endregion

        #region Constructors

        /// <summary>
        /// Protected constructor to prevent direct instantiation.
        /// </summary>
        protected EnumComparer()
        {
            // this could be in static ctor but that could throw a TypeInitializationException at unexpected places
            if (!typeof(TEnum).IsEnum)
                Throw.InvalidOperationException(Res.EnumTypeParameterInvalid);
        }

        #endregion

        #region Methods

        #region Public Methods

        /// <summary>
        /// Determines whether the specified <paramref name="obj"/> is the same type of <see cref="EnumComparer{TEnum}"/> as the current instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns><see langword="true"/> if the specified <see cref="object" /> is equal to this instance; otherwise, <see langword="false"/>.</returns>
        public override bool Equals(object? obj) => obj is EnumComparer<TEnum>;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() => typeof(EnumComparer<TEnum>).GetHashCode();

        /// <summary>
        /// Determines whether two <typeparamref name="TEnum"/> instances are equal.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the specified values are equal; otherwise, <see langword="false"/>.
        /// </returns>
        /// <param name="x">The first <typeparamref name="TEnum"/> value to compare.</param>
        /// <param name="y">The second <typeparamref name="TEnum"/> value to compare.</param>
        public abstract bool Equals(TEnum x, TEnum y);

        /// <summary>
        /// Returns a hash code for the specified <typeparamref name="TEnum"/> instance.
        /// </summary>
        /// <returns>
        /// A hash code for the specified <typeparamref name="TEnum"/> instance.
        /// </returns>
        /// <param name="obj">The <typeparamref name="TEnum"/> for which a hash code is to be returned.</param>
        /// <remarks>Returned hash code is not necessarily equals with own hash code of an <see langword="enum"/> value but provides a fast and well-spread value.</remarks>
        public abstract int GetHashCode(TEnum obj);

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
        public abstract int Compare(TEnum x, TEnum y);

        #endregion

        #region Protected-Internal Methods        

        /// <summary>
        /// Converts an <see cref="ulong">ulong</see> value to <typeparamref name="TEnum"/>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The <typeparamref name="TEnum"/> representation of <paramref name="value"/>.</returns>
        protected internal abstract TEnum ToEnum(ulong value);

        /// <summary>
        /// Converts a <typeparamref name="TEnum"/> value to <see cref="ulong">ulong</see>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The <see cref="ulong">ulong</see> representation of <typeparamref name="TEnum"/>.</returns>
        protected internal abstract ulong ToUInt64(TEnum value);

        /// <summary>
        /// Converts a <typeparamref name="TEnum"/> value to <see cref="long">long</see>.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The <see cref="long">ulong</see> representation of <typeparamref name="TEnum"/>.</returns>
        protected internal abstract long ToInt64(TEnum value);

        #endregion

        #region Explicitly Implemented Interface Methods

        [SecurityCritical]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) => info.SetType(typeof(SerializationUnityHolder));

        #endregion

        #endregion
    }
}
