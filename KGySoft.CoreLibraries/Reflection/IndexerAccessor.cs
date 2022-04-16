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

using KGySoft.CoreLibraries;

#region Used Namespaces

using System;
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;

#endregion

#region Used Aliases

using NonGenericSetter = System.Action<object?, object?, object?[]>;
using NonGenericGetter = System.Func<object?, object?[], object?>;

#endregion

#endregion

namespace KGySoft.Reflection
{
    internal sealed class IndexerAccessor : PropertyAccessor
    {
        #region Constructors

        internal IndexerAccessor(PropertyInfo pi)
            : base(pi)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override void Set(object? instance, object? value, params object?[]? indexParameters)
            => ((NonGenericSetter)Setter)(instance, value, indexParameters ?? Throw.ArgumentNullException<object[]>(Argument.indexParameters));

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override object? Get(object? instance, params object?[]? indexParameters)
            => ((NonGenericGetter)Getter)(instance, indexParameters ?? Throw.ArgumentNullException<object[]>(Argument.indexParameters));

        #endregion

        #region Private Protected Methods

        private protected override Delegate CreateGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
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
                return dm.CreateDelegate(typeof(NonGenericGetter));
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

            LambdaExpression lambda = Expression.Lambda<NonGenericGetter>(
                Expression.Convert(getterCall, Reflector.ObjectType), // object return type
                instanceParameter, // instance (object)
                indexArgumentsParameter); // index parameters (object[])
            return lambda.Compile();
        }

        private protected override Delegate CreateSetter()
        {
            if (!CanWrite)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
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
                return dm.CreateDelegate(typeof(NonGenericSetter));
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

            LambdaExpression lambda = Expression.Lambda<NonGenericSetter>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter, // value (object)
                indexArgumentsParameter); // index parameters (object[])
            return lambda.Compile();
        }

        private protected override Delegate CreateGenericGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            var property = (PropertyInfo)MemberInfo;
            MethodInfo getterMethod = property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(Res.ReflectionCannotInvokeIndexerGeneric);

            bool isValueType = declaringType.IsValueType;
            ParameterExpression instanceParameter = Expression.Parameter(isValueType ? declaringType.MakeByRefType() : declaringType, "instance");
            ParameterExpression indexParameter = Expression.Parameter(ParameterTypes[0], "index");
            MethodCallExpression getterCall = Expression.Call(instanceParameter, getterMethod, indexParameter);
            Type delegateType = (isValueType ? typeof(ValueTypeFunction<,,>) : typeof(Func<,,>))
                .GetGenericType(declaringType, indexParameter.Type, property.PropertyType);
            LambdaExpression lambda = Expression.Lambda(delegateType, getterCall, instanceParameter, indexParameter);
            return lambda.Compile();
        }

        private protected override Delegate CreateGenericSetter()
        {
            if (!CanWrite)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            var property = (PropertyInfo)MemberInfo;
            MethodInfo setterMethod = property.GetSetMethod(true)!;
            Type? declaringType = setterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(Res.ReflectionCannotInvokeIndexerGeneric);

            bool isValueType = declaringType.IsValueType;
            ParameterExpression instanceParameter = Expression.Parameter(isValueType ? declaringType.MakeByRefType() : declaringType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(property.PropertyType, "value");
            ParameterExpression indexParameter = Expression.Parameter(ParameterTypes[0], "index");
            MethodCallExpression setterCall = Expression.Call(instanceParameter, setterMethod, valueParameter, indexParameter);
            Type delegateType = (isValueType ? typeof(ValueTypeAction<,,>) : typeof(Action<,,>))
                .GetGenericType(declaringType, property.PropertyType, indexParameter.Type);
            LambdaExpression lambda = Expression.Lambda(delegateType, setterCall, instanceParameter, valueParameter, indexParameter);
            return lambda.Compile();
        }

        #endregion

        #endregion
    }
}
