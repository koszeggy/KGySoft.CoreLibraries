#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SimplePropertyAccessor.cs
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

#region Used Namespaces

using System;
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;

using KGySoft.CoreLibraries;

#endregion

#region Used Aliases

using NonGenericSetter = System.Action<object?, object?>;
using NonGenericGetter = System.Func<object?, object?>;

#endregion

#endregion

namespace KGySoft.Reflection
{
    internal sealed class SimplePropertyAccessor : PropertyAccessor
    {
        #region Constructors

        internal SimplePropertyAccessor(PropertyInfo pi)
            : base(pi)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override void Set(object? instance, object? value, params object?[]? indexParameters)
            => ((NonGenericSetter)Setter).Invoke(instance, value);

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override object? Get(object? instance, params object?[]? indexParameters)
            => ((NonGenericGetter)Getter).Invoke(instance);

        #endregion

        #region Private Protected Methods

        private protected override Delegate CreateGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo getterMethod = property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));

#if !NETSTANDARD2_0
            // for struct instance properties: Dynamic method
            if (declaringType.IsValueType && !getterMethod.IsStatic)
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.OmitParameters);
                return dm.CreateDelegate(typeof(NonGenericGetter));
            }
#endif

            // For classes and static properties: Lambda expression (.NET Standard 2.0: also for structs, mutated content might be lost)
            //---by property expression---
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");

            MemberExpression member = Expression.Property(
                getterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                (PropertyInfo)MemberInfo);

            LambdaExpression lambda = Expression.Lambda<NonGenericGetter>(
                Expression.Convert(member, Reflector.ObjectType), // object return type
                instanceParameter); // instance (object)
            return lambda.Compile();

            ////---by calling the getter method---
            //ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");

            //MethodCallExpression getterCall = Expression.Call(
            //    Expression.Convert(instanceParameter, declaringType), // (TDeclaring)target
            //    getterMethod); // getter

            //LambdaExpression lambda = Expression.Lambda<PropertyGetter>(
            //    Expression.Convert(getterCall, Reflector.ObjectType), // object return type
            //    instanceParameter);   // instance (object)
            //return lambda.Compile();
        }

        private protected override Delegate CreateSetter()
        {
            if (!CanWrite)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo setterMethod = property.GetSetMethod(true)!;
            Type? declaringType = setterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));

            if (declaringType.IsValueType && !setterMethod.IsStatic)
            {
#if NETSTANDARD2_0
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructPropertyNetStandard20(property.Name, declaringType));
#else
                // for struct instance properties: Dynamic method
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
                return dm.CreateDelegate(typeof(NonGenericSetter));
#endif
            }

            // for classes and static properties: Lambda expression
            // Calling the setter method (works even in .NET 3.5, while Assign is available from .NET 4 only)
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            UnaryExpression castValue = Expression.Convert(valueParameter, property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                setterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                setterMethod, // setter
                castValue); // original parameter: (TProp)value

            LambdaExpression lambda = Expression.Lambda<NonGenericSetter>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter);
            return lambda.Compile();
        }

        private protected override Delegate CreateGenericGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));

            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo getterMethod = property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));

            MethodCallExpression getterCall;
            ParameterExpression instanceParameter;
            LambdaExpression lambda;

            // Static properties
            if (getterMethod.IsStatic)
            {
                getterCall = Expression.Call(null, getterMethod);
                lambda = Expression.Lambda(typeof(Func<>).GetGenericType(property.PropertyType), getterCall);
                return lambda.Compile();
            }

            // Class instance properties
            if (!declaringType.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                getterCall = Expression.Call(instanceParameter, getterMethod);
                lambda = Expression.Lambda(typeof(Func<,>).GetGenericType(declaringType, property.PropertyType), getterCall, instanceParameter);
                return lambda.Compile();
            }

            // Struct instance properties
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            getterCall = Expression.Call(instanceParameter, getterMethod);
            lambda = Expression.Lambda(typeof(ValueTypeFunction<,>).GetGenericType(declaringType, property.PropertyType), getterCall, instanceParameter);
            return lambda.Compile();
        }

        private protected override Delegate CreateGenericSetter()
        {
            if (!CanWrite)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));

            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo setterMethod = property.GetSetMethod(true)!;
            Type? declaringType = setterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));

            ParameterExpression instanceParameter;
            ParameterExpression valueParameter;
            MethodCallExpression setterCall;
            LambdaExpression lambda;

            // Static properties
            if (setterMethod.IsStatic)
            {
                valueParameter = Expression.Parameter(property.PropertyType, "value");
                setterCall = Expression.Call(null, setterMethod, valueParameter);
                lambda = Expression.Lambda(typeof(Action<>).GetGenericType(property.PropertyType), setterCall, valueParameter);
                return lambda.Compile();
            }

            // Class instance properties
            if (!declaringType.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                valueParameter = Expression.Parameter(property.PropertyType, "value");
                setterCall = Expression.Call(instanceParameter, setterMethod, valueParameter);
                lambda = Expression.Lambda(typeof(Action<,>).GetGenericType(declaringType, property.PropertyType), setterCall, instanceParameter, valueParameter);
                return lambda.Compile();
            }

            // Struct instance properties
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            valueParameter = Expression.Parameter(property.PropertyType, "value");
            setterCall = Expression.Call(instanceParameter, setterMethod, valueParameter);
            lambda = Expression.Lambda(typeof(ValueTypeAction<,>).GetGenericType(declaringType, property.PropertyType), setterCall, instanceParameter, valueParameter);
            return lambda.Compile();
        }

        #endregion

        #endregion
    }
}
