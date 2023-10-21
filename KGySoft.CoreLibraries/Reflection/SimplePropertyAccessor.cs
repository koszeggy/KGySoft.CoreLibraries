#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SimplePropertyAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
#if NETSTANDARD2_0
using System.Linq.Expressions;
#endif
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif

using KGySoft.CoreLibraries;

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

        #region Private Protected Methods

        private protected override Action<object?, object?, object?[]?> CreateGeneralSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            if (!CanWrite)
            {
                if (Property.PropertyType.IsByRef)
                {
#if NETSTANDARD2_0
                    Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
                    DynamicMethod dm = CreateSetRefAsDynamicMethod(null);
                    return (Action<object?, object?, object?[]?>)dm.CreateDelegate(typeof(Action<object?, object?, object?[]?>));
#endif
                }

                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            }

            MethodInfo setterMethod = Property.GetSetMethod(true)!;
            if (!setterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

#if NETSTANDARD2_0
            if (!setterMethod.IsStatic && declaringType?.IsValueType == true)
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructPropertyNetStandard20(Property.Name, Property.PropertyType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            ParameterExpression indexParametersParameter = Expression.Parameter(typeof(object[]), "indexParameters");
            UnaryExpression castValue = Expression.Convert(valueParameter, Property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                setterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                setterMethod, // setter
                castValue); // original parameter: (TProp)value

            var lambda = Expression.Lambda<Action<object?, object?, object?[]?>>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter, // value (object)
                indexParametersParameter); // indexParameters (object[]) - ignored
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
            return (Action<object?, object?, object?[]?>)result.CreateDelegate(typeof(Action<object?, object?, object?[]?>));
#endif
        }

        private protected override Func<object?, object?[]?, object?> CreateGeneralGetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            if (!getterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

#if NETSTANDARD2_0
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression indexParametersParameter = Expression.Parameter(typeof(object[]), "indexParameters");

            MemberExpression member = Expression.Property(
                getterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                Property);

            var lambda = Expression.Lambda<Func<object?, object?[]?, object?>>(
                Expression.Convert(member, Reflector.ObjectType), // object return type
                instanceParameter, // instance (object)
                indexParametersParameter); // indexParameters (object[]) - ignored
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.None);
            return (Func<object?, object?[]?, object?>)dm.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            if (!CanWrite)
            {
                if (Property.PropertyType.IsByRef)
                {
#if NETSTANDARD2_0
                    Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
                    DynamicMethod dm = CreateSetRefAsDynamicMethod(false);
                    return (Action<object?, object?>)dm.CreateDelegate(typeof(Action<object?, object?>));
#endif
                }

                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            }

            MethodInfo setterMethod = Property.GetSetMethod(true)!;
            if (!setterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

#if NETSTANDARD2_0
            if (!setterMethod.IsStatic && declaringType?.IsValueType == true)
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructPropertyNetStandard20(Property.Name, declaringType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            UnaryExpression castValue = Expression.Convert(valueParameter, Property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                setterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                setterMethod, // setter
                castValue); // original parameter: (TProp)value

            var lambda = Expression.Lambda<Action<object?, object?>>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter); // value (object)
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters);
            return (Action<object?, object?>)result.CreateDelegate(typeof(Action<object?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericGetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            if (!getterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

#if NETSTANDARD2_0
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            MemberExpression member = Expression.Property(
                getterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                Property);

            var lambda = Expression.Lambda<Func<object?, object?>>(
                Expression.Convert(member, Reflector.ObjectType), // object return type
                instanceParameter);
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.ExactParameters);
            return (Func<object?, object?>)dm.CreateDelegate(typeof(Func<object?, object?>));
#endif
        }

        private protected override Delegate CreateGenericSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            bool isByRef = Property.PropertyType.IsByRef;
            bool isStatic = (isByRef ? Property.GetGetMethod(true) : Property.GetSetMethod(true))!.IsStatic;

            if (!isStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

            Type propertyType = isByRef ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            Type delegateType = isStatic ? typeof(Action<>).GetGenericType(propertyType)
                : declaringType!.IsValueType ? typeof(ValueTypeAction<,>).GetGenericType(declaringType, propertyType)
                : typeof(ReferenceTypeAction<,>).GetGenericType(declaringType, propertyType);

            if (!CanWrite)
            {
                if (isByRef)
                {
#if NETSTANDARD2_0
                    Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
                    DynamicMethod dm = CreateSetRefAsDynamicMethod(true);
                    return dm.CreateDelegate(delegateType);
#endif
                }

                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            }

            MethodInfo setterMethod = Property.GetSetMethod(true)!;

#if NETSTANDARD2_0
            ParameterExpression instanceParameter;
            ParameterExpression valueParameter = Expression.Parameter(Property.PropertyType, "value");
            MethodCallExpression setterCall;
            LambdaExpression lambda;

            // Static property
            if (setterMethod.IsStatic)
            {
                setterCall = Expression.Call(null, setterMethod, valueParameter);
                lambda = Expression.Lambda(delegateType, setterCall, valueParameter);
                return lambda.Compile();
            }

            // Class instance property
            if (!declaringType!.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                setterCall = Expression.Call(instanceParameter, setterMethod, valueParameter);
                lambda = Expression.Lambda(delegateType, setterCall, instanceParameter, valueParameter);
                return lambda.Compile();
            }

            // Struct instance property
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            setterCall = Expression.Call(instanceParameter, setterMethod, valueParameter);
            lambda = Expression.Lambda(delegateType, setterCall, instanceParameter, valueParameter);
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif
        }

        private protected override Delegate CreateGenericGetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));

            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            bool isStatic = getterMethod.IsStatic;
            if (!isStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            bool isByRef = Property.PropertyType.IsByRef;
            Type propertyType = isByRef ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            bool isValueType = declaringType?.IsValueType == true;
            Type delegateType = isStatic
                ? typeof(Func<>).GetGenericType(propertyType)
                : (isValueType ? typeof(ValueTypeFunction<,>) : typeof(ReferenceTypeFunction<,>)).GetGenericType(declaringType!, propertyType);

#if NETSTANDARD2_0
            if (isByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
    
            MethodCallExpression getterCall;
            ParameterExpression instanceParameter;
            LambdaExpression lambda;

            // Static property
            if (getterMethod.IsStatic)
            {
                getterCall = Expression.Call(null, getterMethod);
                lambda = Expression.Lambda(delegateType, getterCall);
                return lambda.Compile();
            }

            // Class instance property
            if (!declaringType!.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                getterCall = Expression.Call(instanceParameter, getterMethod);
                lambda = Expression.Lambda(delegateType, getterCall, instanceParameter);
                return lambda.Compile();
            }

            // Struct instance property
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            getterCall = Expression.Call(instanceParameter, getterMethod);
            lambda = Expression.Lambda(delegateType, getterCall, instanceParameter);
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif
        }

        #endregion

        #region Private Methods

#if !NETSTANDARD2_0
        private DynamicMethod CreateSetRefAsDynamicMethod(bool? generic)
        {
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            bool isStatic = getterMethod.IsStatic;
            Debug.Assert(isStatic || declaringType != null);
            Debug.Assert(getterMethod.ReturnType.IsByRef);
            Type propertyType = getterMethod.ReturnType.GetElementType()!;

            Type[] paramTypes = generic switch
            {
                null => new[] { Reflector.ObjectType, Reflector.ObjectType, typeof(object[]) },
                false => new[] { Reflector.ObjectType, Reflector.ObjectType },
                true => isStatic
                    ? new[] { propertyType }
                    : new[] { declaringType!.IsValueType ? declaringType.MakeByRefType() : declaringType, propertyType }
            };

            var dm = new DynamicMethod("<SetRefProperty>__" + Property.Name, Reflector.VoidType, paramTypes,
                GetOwner(), true);

            ILGenerator ilGenerator = dm.GetILGenerator();

            // if instance property
            if (!isStatic)
            {
                // loading 0th argument (instance)
                ilGenerator.Emit(OpCodes.Ldarg_0);
                if (generic != true)
                    ilGenerator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
            }

            // calling the getter
            ilGenerator.Emit(getterMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getterMethod);

            // loading value argument
            ilGenerator.Emit(isStatic && generic == true ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
            if (generic != true)
                ilGenerator.Emit(propertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, propertyType);

            // setting the returned reference
            if (propertyType.IsValueType)
                ilGenerator.Emit(OpCodes.Stobj, propertyType);
            else
                ilGenerator.Emit(OpCodes.Stind_Ref);

            ilGenerator.Emit(OpCodes.Ret);
            return dm;
        }
#endif

        #endregion

        #endregion
    }
}
