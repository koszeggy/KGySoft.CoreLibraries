#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SimplePropertyAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2026 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Diagnostics.CodeAnalysis;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Linq.Expressions;
#endif
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
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

            if (!Property.CanWrite)
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
            if (!setterMethod.IsStatic && declaringType is null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

#if NETSTANDARD2_0
            // Value type: using reflection as fallback so mutations are preserved. Same for pointer properties that are not supported by Expression trees.
            if (!setterMethod.IsStatic && declaringType!.IsValueType || Property.PropertyType.IsPointer())
                return Property.SetValue;

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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
#if NET8_0_OR_GREATER
                MethodInvoker invoker = FallbackSetter!;
                return (obj, value, _) => invoker.Invoke(obj, value);
#else
                return Property.SetValue;
#endif
            }
#endif
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
            return (Action<object?, object?, object?[]?>)result.CreateDelegate(typeof(Action<object?, object?, object?[]?>));
#endif
        }

        private protected override Func<object?, object?[]?, object?> CreateGeneralGetter()
        {
            #region Local Methods

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Func<object?, object?[]?, object?> SystemReflectionFallback()
            {
                PropertyInfo pi = Property;
                Type propertyType = pi.PropertyType;
                if (propertyType.IsByRef)
                    propertyType = propertyType.GetElementType()!;
                
                unsafe
                {
                    // Only real pointers are returned as Reflection.Pointer, whereas function pointers are returned as IntPtr,
                    // so using the IsPointer property rather than the IsPointer() extension here is intended.
#if NET8_0_OR_GREATER
                    MethodInvoker invoker = FallbackGetter!;
                    return propertyType.IsPointer ? (obj, _) => (IntPtr)Pointer.Unbox(invoker.Invoke(obj)!) : (obj, _) => invoker.Invoke(obj);
#else
                    return propertyType.IsPointer ? (obj, _) => (IntPtr)Pointer.Unbox(pi.GetValue(obj)!) : pi.GetValue;
#endif
                }
            }
#endif

            #endregion

            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            if (!getterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

#if NETSTANDARD2_0
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            // Non-readonly value type: using reflection as fallback so mutations are preserved. Same for pointer properties that are not supported by Expression trees.
            if (!getterMethod.IsStatic && declaringType!.IsValueType && !(declaringType.IsReadOnly() || getterMethod.IsReadOnly()) || Property.PropertyType.IsPointer())
                return SystemReflectionFallback();

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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
                return SystemReflectionFallback();
#endif
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.None);
            return (Func<object?, object?[]?, object?>)dm.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);

            if (!Property.CanWrite)
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
            // Value type: using reflection as fallback so mutations are preserved. Same for pointer properties that are not supported by Expression trees.
            if (!setterMethod.IsStatic && declaringType!.IsValueType || Property.PropertyType.IsPointer())
                return new Action<object?, object?>(Property.SetValue);

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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
#if NET8_0_OR_GREATER
                MethodInvoker invoker = FallbackSetter!;
                return new Action<object?, object?>((obj, value) => invoker.Invoke(obj, value));
#else
                return new Action<object?, object?>(Property.SetValue);
#endif
            }
#endif
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters);
            return (Action<object?, object?>)result.CreateDelegate(typeof(Action<object?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericGetter()
        {
            #region Local Methods

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Func<object?, object?> SystemReflectionFallback()
            {
                PropertyInfo pi = Property;
                Type propertyType = pi.PropertyType;
                if (propertyType.IsByRef)
                    propertyType = propertyType.GetElementType()!;

                unsafe
                {
                    // Only real pointers are returned as Reflection.Pointer, whereas function pointers are returned as IntPtr,
                    // so using the IsPointer property rather than the IsPointer() extension here is intended.
#if NET8_0_OR_GREATER
                    MethodInvoker invoker = FallbackGetter!;
                    return propertyType.IsPointer ? obj => (IntPtr)Pointer.Unbox(invoker.Invoke(obj)!) : invoker.Invoke;
#else
                    return propertyType.IsPointer ? obj => (IntPtr)Pointer.Unbox(pi.GetValue(obj)!) : pi.GetValue;
#endif
                }
            }
#endif

            #endregion

            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            if (!getterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

#if NETSTANDARD2_0
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            // Non-readonly value type: using reflection as fallback so mutations are preserved
            if (!getterMethod.IsStatic && declaringType!.IsValueType && !(declaringType.IsReadOnly() || getterMethod.IsReadOnly()) || Property.PropertyType.IsPointer())
                return SystemReflectionFallback();

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            MemberExpression member = Expression.Property(
                getterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                Property);

            var lambda = Expression.Lambda<Func<object?, object?>>(
                Expression.Convert(member, Reflector.ObjectType), // object return type
                instanceParameter);
            return lambda.Compile();
#else
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
                return SystemReflectionFallback();
#endif
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.ExactParameters);
            return (Func<object?, object?>)dm.CreateDelegate(typeof(Func<object?, object?>));
#endif
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification = "False alarm, the new analyzer includes the complexity of local methods - see https://github.com/dotnet/roslyn-analyzers/issues/2934")]
        private protected override Delegate CreateGenericSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);

            bool isByRef = Property.PropertyType.IsByRef;
            bool isStatic = (isByRef ? Property.GetGetMethod(true) : Property.GetSetMethod(true))!.IsStatic;

            if (!isStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

            Type propertyType = isByRef ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            bool isPointer = propertyType.IsPointer();
            if (isPointer)
                propertyType = typeof(IntPtr);

            Type delegateType = isStatic ? typeof(Action<>).GetGenericType(propertyType)
                : declaringType!.IsValueType ? typeof(ValueTypeAction<,>).GetGenericType(declaringType, propertyType)
                : typeof(ReferenceTypeAction<,>).GetGenericType(declaringType, propertyType);

            if (!Property.CanWrite)
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

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#if !NETSTANDARD2_0
            // Dynamic methods and IL generation are not supported: fallback to Expressions.
            // In AOT mode it will work in interpreted mode, which is even slower than the non-generic alternative...
            if (!RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                return CreateByExpressions();
            }
#endif

#if !NETSTANDARD2_0
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif

            #region Local Methods

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Delegate CreateByExpressions()
            {
                ParameterExpression instanceParameter;
                MethodCallExpression setterCall;
                LambdaExpression lambda;

                // Pointer property: fallback to System reflection, which supports pointer parameters as IntPtr.
                if (isPointer)
                {
                    bool isValueType = declaringType?.IsValueType == true;

                    // value types: though we can call SetValue(object,object), the ref instance parameter gets boxed in a new object, losing all mutations
                    if (isValueType && !isStatic && !declaringType!.IsReadOnly() && !setterMethod.IsReadOnly())
                        ThrowMutableStructMembersNotSupported();

                    ParameterExpression[] parameters = new ParameterExpression[isStatic ? 1 : 2];
                    int valueIndex = isStatic ? 0 : 1;
                    if (!isStatic)
                        parameters[0] = Expression.Parameter(isValueType ? declaringType!.MakeByRefType() : declaringType!, "instance");
                    parameters[valueIndex] = Expression.Parameter(propertyType, "value");

                    Expression[] methodParameters = new Expression[2];
                    methodParameters[0] = isStatic ? Expression.Constant(null, typeof(object))
                        : parameters[0].Type == typeof(object) ? parameters[0]
                        : Expression.Convert(parameters[0], typeof(object));
                    methodParameters[1] = parameters[valueIndex].Type == typeof(object) ? parameters[valueIndex] : Expression.Convert(parameters[valueIndex], typeof(object));

#if NET8_0_OR_GREATER
                    // fallback to MethodInvoker
                    MethodInvoker invoker = FallbackSetter!;
                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(invoker), // the instance is the MethodInvoker created from the setter
                        invoker.GetType().GetMethod(nameof(MethodInvoker.Invoke), [typeof(object), typeof(object)])!, // Invoke(obj, value)
                        methodParameters);
#else
                    // fallback to PropertyInfo.SetValue(object,object)
                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(Property), // the instance is the PropertyInfo itself
                        Property.GetType().GetMethod(nameof(PropertyInfo.SetValue), [typeof(object), typeof(object)])!, // SetValue(obj, value)
                        methodParameters);
#endif

                    lambda = Expression.Lambda(delegateType, methodCall, parameters);
                    return lambda.Compile();
                }

                // Static property
                ParameterExpression valueParameter = Expression.Parameter(propertyType, "value");
                if (setterMethod.IsStatic)
                {
                    setterCall = Expression.Call(null, setterMethod, valueParameter);
                    lambda = Expression.Lambda(delegateType, setterCall, valueParameter);
                    return lambda.Compile();
                }

                // Instance property
                instanceParameter = declaringType!.IsValueType
                    ? Expression.Parameter(declaringType.MakeByRefType(), "instance")
                    : Expression.Parameter(declaringType, "instance");

                setterCall = Expression.Call(instanceParameter, setterMethod, valueParameter);
                lambda = Expression.Lambda(delegateType, setterCall, instanceParameter, valueParameter);
                return lambda.Compile();
            }
#endif

            #endregion
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification = "False alarm, the new analyzer includes the complexity of local methods - see https://github.com/dotnet/roslyn-analyzers/issues/2934")]
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

            bool isByRef = Property.PropertyType.IsByRef;
            Type propertyType = isByRef ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            bool isPointer = propertyType.IsPointer();
            if (isPointer)
                propertyType = typeof(IntPtr);
            bool isValueType = declaringType?.IsValueType == true;
            Type delegateType = isStatic
                ? typeof(Func<>).GetGenericType(propertyType)
                : (isValueType ? typeof(ValueTypeFunction<,>) : typeof(ReferenceTypeFunction<,>)).GetGenericType(declaringType!, propertyType);

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#if NETSTANDARD2_0
            if (isByRef) // not even the fallback supports ref returns below .NET Core 3.0
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
            // Dynamic methods and IL generation are not supported: fallback to Expressions.
            // In AOT mode it will work in interpreted mode, which is even slower than the non-generic alternative...
            if (!RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                return CreateByExpressions();
            }
#endif
#if !NETSTANDARD2_0
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif

            #region Local Methods

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Delegate CreateByExpressions()
            {
                MethodCallExpression getterCall;
                ParameterExpression instanceParameter;
                LambdaExpression lambda;

                // Pointer property: fallback to System reflection, which supports pointers as IntPtr.
                if (isPointer || isByRef)
                {
                    // value types: though we can call NonGenericGetter.Invoke(object), the ref instance parameter gets boxed in a new object, losing all mutations
                    if (isValueType && !isStatic && !declaringType!.IsReadOnly() && !getterMethod.IsReadOnly())
                        ThrowMutableStructMembersNotSupported();

                    ParameterExpression[] parameters = new ParameterExpression[isStatic ? 0 : 1];
                    if (!isStatic)
                        parameters[0] = Expression.Parameter(isValueType ? declaringType!.MakeByRefType() : declaringType!, "instance");

                    Expression[] methodParameters = new Expression[1];
                    methodParameters[0] = isStatic ? Expression.Constant(null, typeof(object))
                        : parameters[0].Type == typeof(object) ? parameters[0]
                        : Expression.Convert(parameters[0], typeof(object));

                    // NOTE: If the return type is pointer, we should call Pointer.Unbox on the PropertyInfo.GetValue result, which is not possible by expression trees.
                    // So we use the NonGenericGetter delegate for pointer return types, whose Invoke has the same signature as PropertyInfo.GetValue(object),
                    // and it converts the pointer result to IntPtr.
#if NET8_0_OR_GREATER
                    object callTarget = isPointer ? NonGenericGetter : FallbackGetter!;
                    const string getterName = nameof(MethodInvoker.Invoke);
#else
                    object callTarget = isPointer ? NonGenericGetter : Property;
                    string getterName = isPointer ? nameof(Func<,>.Invoke) : nameof(PropertyInfo.GetValue);
#endif

                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(callTarget),
                        callTarget.GetType().GetMethod(getterName, [typeof(object)])!,
                        methodParameters);

                    lambda = Expression.Lambda(delegateType, propertyType == typeof(object) ? methodCall : Expression.Convert(methodCall, propertyType), parameters);
                    return lambda.Compile();
                }

                // Static property
                if (getterMethod.IsStatic)
                {
                    getterCall = Expression.Call(null, getterMethod);
                    lambda = Expression.Lambda(delegateType, getterCall);
                    return lambda.Compile();
                }

                // Instance property
                instanceParameter = declaringType!.IsValueType
                    ? Expression.Parameter(declaringType.MakeByRefType(), "instance")
                    : Expression.Parameter(declaringType, "instance");

                getterCall = Expression.Call(instanceParameter, getterMethod);
                lambda = Expression.Lambda(delegateType, getterCall, instanceParameter);
                return lambda.Compile();
            }
#endif

            #endregion
        }

        #endregion

        #region Private Methods

#if !NETSTANDARD2_0
        private DynamicMethod CreateSetRefAsDynamicMethod(bool? generic)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnSetPropertyAot(Property.PropertyType));
#endif
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            bool isStatic = getterMethod.IsStatic;
            Debug.Assert(isStatic || declaringType != null);
            Debug.Assert(getterMethod.ReturnType.IsByRef);
            Type propertyType = getterMethod.ReturnType.GetElementType()!;
            bool isPointer = propertyType.IsPointer();
            Type valueParameterType = isPointer ? typeof(IntPtr) : propertyType;

            Type[] paramTypes = generic switch
            {
                null => [Reflector.ObjectType, Reflector.ObjectType, typeof(object[])],
                false => [Reflector.ObjectType, Reflector.ObjectType],
                true => isStatic
                    ? [valueParameterType]
                    : [declaringType!.IsValueType ? declaringType.MakeByRefType() : declaringType, valueParameterType]
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
                    ilGenerator.Emit(declaringType!.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);
            }

            // calling the getter
            ilGenerator.Emit(getterMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getterMethod);

            // loading value argument
            ilGenerator.Emit(isStatic && generic == true ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
            if (generic != true)
                ilGenerator.Emit(valueParameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, valueParameterType);

            // setting the returned reference
            if (isPointer)
                ilGenerator.Emit(OpCodes.Stind_I);
            else if (propertyType.IsValueType)
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
