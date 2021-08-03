#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IndexerAccessor.cs
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
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.Reflection
{
    internal sealed class IndexerAccessor : PropertyAccessor
    {
        #region Delegates

        /// <summary>
        /// Represents a non-generic setter that can be used for any indexers.
        /// </summary>
        private delegate void IndexerSetter(object? instance, object? value, object?[] indexArguments);

        /// <summary>
        /// Represents a non-generic getter that can be used for any indexers.
        /// </summary>
        private delegate object? IndexerGetter(object? instance, object?[] indexArguments);

        #endregion

        #region Constructors

        internal IndexerAccessor(PropertyInfo pi)
            : base(pi)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member - in base indexerParameters can be null because it is ignored for simple properties
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override void Set(object? instance, object? value, params object?[] indexerParameters)
            // TODO: indexerParameters actually must not be null here but any check or fallback array initialization would just make the invoke slower.
            // A try-catch and a post check could help to transform the possible exception to a reasonable one without slowing down the happy path
            => ((IndexerSetter)Setter)(instance, value, indexerParameters);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override object? Get(object? instance, params object?[] indexerParameters)
            // TODO: indexerParameters actually must not be null here but any check or fallback array initialization would just make the invoke slower.
            // A try-catch and a post check could help to transform the possible exception to a reasonable one without slowing down the happy path
            => ((IndexerGetter)Getter)(instance, indexerParameters);
#pragma warning restore CS8765

        #endregion

        #region Private Protected Methods

        private protected override Delegate CreateGetter()
        {
            var property = (PropertyInfo)MemberInfo;

            MethodInfo getterMethod = property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));

#if !NETSTANDARD2_0
            // for structs: Dynamic method
            if (declaringType.IsValueType)
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.None);
                return dm.CreateDelegate(typeof(IndexerGetter));
            } 
#endif

            // for classes: Lambda expression
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression indexArgumentsParameter = Expression.Parameter(typeof(object[]), "indexArguments");
            var getterParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
                getterParameters[i] = Expression.Convert(Expression.ArrayIndex(indexArgumentsParameter, Expression.Constant(i)), ParameterTypes[i]);

            MethodCallExpression getterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                getterMethod, // getter
                getterParameters); // arguments cast to target types

            LambdaExpression lambda = Expression.Lambda<IndexerGetter>(
                Expression.Convert(getterCall, Reflector.ObjectType), // object return type
                instanceParameter, // instance (object)
                indexArgumentsParameter); // index parameters (object[])
            return lambda.Compile();
        }

        private protected override Delegate CreateSetter()
        {
            var property = (PropertyInfo)MemberInfo;
            MethodInfo setterMethod = property.GetSetMethod(true)!;
            Type? declaringType = setterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));

            if (declaringType.IsValueType)
            {
#if NETSTANDARD2_0
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructPropertyNetStandard20(property.Name, declaringType));
#else
                // for structs: Dynamic method
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
                return dm.CreateDelegate(typeof(IndexerSetter));
#endif
            }

            // for classes: Lambda expression
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            ParameterExpression indexArgumentsParameter = Expression.Parameter(typeof(object[]), "indexArguments");

            // indexer parameters
            var setterParameters = new Expression[ParameterTypes.Length + 1]; // +1: value to set after indices
            for (int i = 0; i < ParameterTypes.Length; i++)
                setterParameters[i] = Expression.Convert(Expression.ArrayIndex(indexArgumentsParameter, Expression.Constant(i)), ParameterTypes[i]);

            // value parameter is the last one
            setterParameters[ParameterTypes.Length] = Expression.Convert(valueParameter, property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                setterMethod, // setter
                setterParameters); // arguments cast to target types + value as last argument cast to property type

            LambdaExpression lambda = Expression.Lambda<IndexerSetter>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter, // value (object)
                indexArgumentsParameter); // index parameters (object[])
            return lambda.Compile();
        }

        #endregion

        #endregion
    }
}
