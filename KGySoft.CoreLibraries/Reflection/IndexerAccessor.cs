#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IndexerAccessor.cs
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
using System.Linq;
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
    internal sealed class IndexerAccessor : PropertyAccessor
    {
        #region Constructors

        internal IndexerAccessor(PropertyInfo pi)
            : base(pi)
        {
        }

        #endregion

        #region Methods

        #region Private Protected Methods

        private protected override Action<object?, object?, object?[]?> CreateGeneralSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
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

#if NETSTANDARD2_0
            // Non-readonly value type or has ref/out/pointer parameters or pointer return type: using reflection as fallback so mutations are preserved,
            // and ref/out parameters are assigned back (though they are not valid C# indexers, along with static indexers)
            ThrowIfHasRefPointerParameters();
            if (!setterMethod.IsStatic && declaringType.IsValueType && !(declaringType.IsReadOnly() || setterMethod.IsReadOnly())
                || setterMethod.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.IsIn || p.IsOut) || p.ParameterType.IsPointer()))
            {
                return Property.SetValue;
            }

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            ParameterExpression indexParametersParameter = Expression.Parameter(typeof(object[]), "indexParameters");

            // indexer parameters
            var setterParameters = new Expression[ParameterTypes.Length + 1]; // +1: value to set after indices
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];

                // for in parameters
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                setterParameters[i] = Expression.Convert(Expression.ArrayIndex(indexParametersParameter, Expression.Constant(i)), parameterType);
            }

            // value parameter is the last one
            setterParameters[ParameterTypes.Length] = Expression.Convert(valueParameter, Property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                setterMethod, // setter
                setterParameters); // arguments cast to target types + value as last argument cast to property type

            var lambda = Expression.Lambda<Action<object?, object?, object?[]?>>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter, // value (object)
                indexParametersParameter); // indexParameters (object[])
            return lambda.Compile();
#else
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                ThrowIfHasRefPointerParameters();
#if NET8_0_OR_GREATER
                MethodInvoker invoker = FallbackSetter!;
                return (obj, value, args) => invoker.Invoke(obj, [..args.AsSpan(), value]);
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
                    return propertyType.IsPointer ? (obj, args) => (IntPtr)Pointer.Unbox(invoker.Invoke(obj, args.AsSpan())!) : (obj, args) => invoker.Invoke(obj, args.AsSpan());
#else
                    return propertyType.IsPointer ? (obj, args) => (IntPtr)Pointer.Unbox(pi.GetValue(obj, args)!) : pi.GetValue;
#endif
                }
            }
#endif

            #endregion

            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;

#if NETSTANDARD2_0
            ThrowIfHasRefPointerParameters();
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            // Non-readonly value type or has ref/out/pointer parameters or pointer return type: using reflection as fallback so mutations are preserved,
            // and ref/out parameters are assigned back (though they are not valid C# indexers, along with static indexers)
            if (!getterMethod.IsStatic && declaringType.IsValueType && !(declaringType.IsReadOnly() || getterMethod.IsReadOnly()) || Property.PropertyType.IsPointer()
                || getterMethod.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.IsIn || p.IsOut) || p.ParameterType.IsPointer()))
            {
                return SystemReflectionFallback();
            }

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression indexParametersParameter = Expression.Parameter(typeof(object[]), "indexParameters");
            var getterParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];

                // for in parameters
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                getterParameters[i] = Expression.Convert(Expression.ArrayIndex(indexParametersParameter, Expression.Constant(i)), parameterType);
            }

            MethodCallExpression getterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                getterMethod, // getter
                getterParameters); // arguments cast to target types

            var lambda = Expression.Lambda<Func<object?, object?[]?, object?>>(
                Expression.Convert(getterCall, Reflector.ObjectType), // object return type
                instanceParameter, // instance (object)
                indexParametersParameter); // indexParameters (object[])
            return lambda.Compile();
#else
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                ThrowIfHasRefPointerParameters();
                return SystemReflectionFallback();
            }
#endif
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.None);
            return (Func<object?, object?[]?, object?>)dm.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);

            // The 1 parameter overload was called for a more-params indexer
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(); // Will be handled in PostValidate

            if (!Property.CanWrite)
            {
                if (Property.PropertyType.IsByRef)
                {
#if NETSTANDARD2_0
                    Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
                    DynamicMethod dm = CreateSetRefAsDynamicMethod(false);
                    return (Action<object?, object?, object?>)dm.CreateDelegate(typeof(Action<object?, object?, object?>));
#endif
                }

                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            }

            MethodInfo setterMethod = Property.GetSetMethod(true)!;

#if NETSTANDARD2_0
            // Non-readonly value type or has ref/out/pointer parameters or pointer return type: using reflection as fallback so mutations are preserved,
            // and ref/out parameters are assigned back (though they are not valid C# indexers, along with static indexers)
            ThrowIfHasRefPointerParameters();
            if (!setterMethod.IsStatic && declaringType.IsValueType && !(declaringType.IsReadOnly() || setterMethod.IsReadOnly())
                || setterMethod.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.IsIn || p.IsOut) || p.ParameterType.IsPointer()))
            {
                return new Action<object?, object?, object?>((o, v, i) => Property.SetValue(o, v, [i]));
            }

            // for classes: Lambda expression
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            ParameterExpression indexParameter = Expression.Parameter(Reflector.ObjectType, "index");

            // indexer parameters
            var setterParameters = new Expression[2]; // index, value
            setterParameters[0] = Expression.Convert(indexParameter, ParameterTypes[0].IsByRef ? ParameterTypes[0].GetElementType()! : ParameterTypes[0]);
            setterParameters[1] = Expression.Convert(valueParameter, Property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                setterMethod, // setter
                setterParameters); // arguments cast to target types + value as last argument cast to property type

            var lambda = Expression.Lambda<Action<object?, object?, object?>>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter, // value (object)
                indexParameter); // index (object)
            return lambda.Compile();
#else
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                ThrowIfHasRefPointerParameters();
#if NET8_0_OR_GREATER
                MethodInvoker invoker = FallbackSetter!;
                return new Action<object?, object?, object?>((o, v, i) => invoker.Invoke(o, i, v));
#else
                return new Action<object?, object?, object?>((o, v, i) => Property.SetValue(o, v, [i]));
#endif
            }
#endif
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters);
            return (Action<object?, object?, object?>)result.CreateDelegate(typeof(Action<object?, object?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericGetter()
        {
            #region Local Methods

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Func<object?, object?, object?> SystemReflectionFallback()
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
                    return propertyType.IsPointer ? (o, i) => (IntPtr)Pointer.Unbox(invoker.Invoke(o, i)!) : invoker.Invoke;
#else
                    return propertyType.IsPointer ? (o, i) => (IntPtr)Pointer.Unbox(pi.GetValue(o, [i])!) : (o, i) => pi.GetValue(o, [i]);
#endif
                }
            }
#endif

            #endregion

            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));

            // The 1 parameter overload was called for a more-params indexer
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(); // Will be handled in PostValidate

            MethodInfo getterMethod = Property.GetGetMethod(true)!;

#if NETSTANDARD2_0
            ThrowIfHasRefPointerParameters();
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            // Non-readonly value type or has ref/out/pointer parameters or pointer return type: using reflection as fallback so mutations are preserved,
            // and ref/out parameters are assigned back (though they are not valid C# indexers, along with static indexers)
            if (!getterMethod.IsStatic && declaringType.IsValueType && !(declaringType.IsReadOnly() || getterMethod.IsReadOnly()) || Property.PropertyType.IsPointer
                || getterMethod.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.IsIn || p.IsOut) || p.ParameterType.IsPointer()))
            {
                return SystemReflectionFallback();
            }

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression indexParameter = Expression.Parameter(Reflector.ObjectType, "index");

            MethodCallExpression getterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                getterMethod, // getter
                Expression.Convert(indexParameter, ParameterTypes[0].IsByRef ? ParameterTypes[0].GetElementType()! : ParameterTypes[0])); // index cast to the parameter type

            var lambda = Expression.Lambda<Func<object?, object?, object?>>(
                Expression.Convert(getterCall, Reflector.ObjectType), // object return type
                instanceParameter, // instance (object)
                indexParameter); // index (object)
            return lambda.Compile();
#else
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                ThrowIfHasRefPointerParameters();
                return SystemReflectionFallback();
            }
#endif
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.ExactParameters);
            return (Func<object?, object?, object?>)dm.CreateDelegate(typeof(Func<object?, object?, object?>));
#endif
        }

        private protected override Delegate CreateGenericSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(Res.ReflectionIndexerGenericNotSupported);

            bool isByRef = Property.PropertyType.IsByRef;
            bool isValueType = declaringType.IsValueType;
            Type propertyType = isByRef ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            bool isPointer = propertyType.IsPointer();
            if (isPointer)
                propertyType = typeof(IntPtr);
            Type indexType = ParameterTypes[0];
            if (indexType.IsByRef)
                indexType = indexType.GetElementType()!;
            bool isPointerIndex = indexType.IsPointer();
            if (isPointerIndex)
                indexType = typeof(IntPtr);
            Type delegateType = (isValueType ? typeof(ValueTypeAction<,,>) : typeof(ReferenceTypeAction<,,>))
                .GetGenericType(GetGenericArguments([declaringType, propertyType, indexType]).ToArray());

            if (!Property.CanWrite)
            {
                if (Property.PropertyType.IsByRef)
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
                ParameterExpression instanceParameter = Expression.Parameter(isValueType ? declaringType.MakeByRefType() : declaringType, "instance");
                ParameterExpression indexParameter = Expression.Parameter(indexType, "index");
                ParameterExpression valueParameter = Expression.Parameter(propertyType, "value");
                LambdaExpression lambda;

                // Pointer indexer: fallback to System reflection, which supports pointer parameters as IntPtr.
                ThrowIfHasRefPointerParameters();
                if (isPointer || isPointerIndex)
                {
                    // value types: though we can call SetValue(object,object,object[]), the ref instance parameter gets boxed in a new object, losing all mutations
                    if (isValueType && !declaringType.IsReadOnly() && !setterMethod.IsReadOnly())
                        ThrowMutableStructMembersNotSupported();

                    Expression[] methodParameters = new Expression[3];
                    methodParameters[0] = instanceParameter.Type == typeof(object)
                        ? instanceParameter
                        : Expression.Convert(instanceParameter, typeof(object));

#if NET8_0_OR_GREATER
                    // fallback to MethodInvoker
                    MethodInvoker invoker = FallbackSetter!;
                    methodParameters[1] = indexParameter.Type == typeof(object) ? indexParameter : Expression.Convert(indexParameter, typeof(object));
                    methodParameters[2] = valueParameter.Type == typeof(object) ? valueParameter : Expression.Convert(valueParameter, typeof(object));

                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(invoker), // the instance is the MethodInvoker created from the setter
                        invoker.GetType().GetMethod(nameof(MethodInvoker.Invoke), [typeof(object), typeof(object), typeof(object)])!, // Invoke(obj, index, value)
                        methodParameters);
#else
                    // fallback to PropertyInfo.SetValue(object,object,object[])
                    methodParameters[1] = valueParameter.Type == typeof(object) ? valueParameter : Expression.Convert(valueParameter, typeof(object));
                    methodParameters[2] = Expression.NewArrayInit(typeof(object), indexParameter.Type == typeof(object) ? indexParameter : Expression.Convert(indexParameter, typeof(object)));

                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(Property), // the instance is the PropertyInfo itself
                        Property.GetType().GetMethod(nameof(PropertyInfo.SetValue), [typeof(object), typeof(object), typeof(object[])])!, // SetValue(obj, value, [index])
                        methodParameters);
#endif

                    lambda = Expression.Lambda(delegateType, methodCall, instanceParameter, valueParameter, indexParameter);
                    return lambda.Compile();
                }

                // note that in the setter method the index comes first, then the value to set (as opposed to PropertyInfo.SetValue where the value comes first)
                MethodCallExpression setterCall = Expression.Call(instanceParameter, setterMethod, indexParameter, valueParameter);
                lambda = Expression.Lambda(delegateType, setterCall, instanceParameter, valueParameter, indexParameter);
                return lambda.Compile();
            }
#endif

#if !NETSTANDARD2_0
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif
        }

        private protected override Delegate CreateGenericGetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(Res.ReflectionIndexerGenericNotSupported);

            bool isValueType = declaringType.IsValueType;
            bool isRefReturn = Property.PropertyType.IsByRef;
            Type returnType = isRefReturn ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            bool isPointer = returnType.IsPointer();
            if (isPointer)
                returnType = typeof(IntPtr);
            Type indexType = ParameterTypes[0];
            if (indexType.IsByRef)
                indexType = indexType.GetElementType()!;
            bool isPointerIndex = indexType.IsPointer();
            if (isPointerIndex)
                indexType = typeof(IntPtr);
            Type delegateType = (isValueType ? typeof(ValueTypeFunction<,,>) : typeof(ReferenceTypeFunction<,,>))
                .GetGenericType(GetGenericArguments([declaringType, indexType, returnType]).ToArray());

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#if NETSTANDARD2_0
            if (isRefReturn) // not even the fallback supports ref returns below .NET Core 3.0
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
            // Dynamic methods and IL generation are not supported: fallback to Expressions.
            // In AOT mode it will work in interpreted mode, which is even slower than the non-generic alternative...
            if (!RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                ParameterExpression instanceParameter = Expression.Parameter(isValueType ? declaringType.MakeByRefType() : declaringType, "instance");
                ParameterExpression indexParameter = Expression.Parameter(indexType, "index");
                LambdaExpression lambda;

                // Pointer property: fallback to System reflection, which supports pointers as IntPtr.
                ThrowIfHasRefPointerParameters();
                if (isPointer || isPointerIndex || isRefReturn)
                {
                    // value types: though we can call NonGenericGetter.Invoke(object), the ref instance parameter gets boxed in a new object, losing all mutations
                    if (isValueType && !declaringType.IsReadOnly() && !getterMethod.IsReadOnly())
                        ThrowMutableStructMembersNotSupported();

                    Expression[] methodParameters = new Expression[2];
                    methodParameters[0] = instanceParameter.Type == typeof(object)
                        ? instanceParameter
                        : Expression.Convert(instanceParameter, typeof(object));

#if NET8_0_OR_GREATER
                    methodParameters[1] = indexParameter.Type == typeof(object) ? indexParameter : Expression.Convert(indexParameter, typeof(object));

                    // NOTE: If the return type is pointer, we should call Pointer.Unbox on the PropertyInfo.GetValue result, which is not possible by expression trees.
                    // Sow we use the NonGenericGetter delegate for pointer return types, whose Invoke has the same signature as MethodInvoker.Invoke(object, object),
                    // and it converts the pointer result to IntPtr.
                    object callTarget = isPointer ? NonGenericGetter : FallbackGetter!;
                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(callTarget),
                        callTarget.GetType().GetMethod(nameof(MethodInvoker.Invoke), [typeof(object), typeof(object)])!,
                        methodParameters);
#else
                    methodParameters[1] = Expression.NewArrayInit(typeof(object), indexParameter.Type == typeof(object) ? indexParameter : Expression.Convert(indexParameter, typeof(object)));

                    // NOTE: If the return type is pointer, we should call Pointer.Unbox on the PropertyInfo.GetValue result, which is not possible by expression trees.
                    // Sow we use the GeneralGetter delegate for pointer return types, whose Invoke has the same signature as PropertyInfo.GetValue(object, object[]),
                    // and it converts the pointer result to IntPtr.
                    object callTarget = isPointer ? GeneralGetter : Property;
                    string getterName = isPointer ? nameof(GeneralGetter.Invoke) : nameof(PropertyInfo.GetValue);
                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(callTarget),
                        callTarget.GetType().GetMethod(getterName, [typeof(object), typeof(object[])])!,
                        methodParameters);
#endif

                    lambda = Expression.Lambda(delegateType, Expression.Convert(methodCall, returnType), instanceParameter, indexParameter);
                    return lambda.Compile();
                }

                // Note: Expression.Call works everywhere but .NET Framework 3.5 if the instance is a ByRef type
                MethodCallExpression getterCall = Expression.Call(instanceParameter, getterMethod, indexParameter);
                lambda = Expression.Lambda(delegateType, getterCall, instanceParameter, indexParameter);
                return lambda.Compile();
            }
#endif

#if !NETSTANDARD2_0
                    DynamicMethod result = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif
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
            Debug.Assert(getterMethod.ReturnType.IsByRef);
            Debug.Assert(generic == null || ParameterTypes.Length == 1, "When creating a specialized delegate only 1 parameter is expected");
            Debug.Assert(declaringType != null);
            if (getterMethod.IsStatic)
                Throw.NotSupportedException(Res.ReflectionRefReturnStaticIndexerNotSupported);

            Type propertyType = getterMethod.ReturnType.GetElementType()!;
            bool isPointer = propertyType.IsPointer();
            Type valueParameterType = isPointer ? typeof(IntPtr) : propertyType;
            Type indexType = ParameterTypes[0]; // the 1st index parameter, used when generic is not null
            if (indexType.IsByRef)
                indexType = indexType.GetElementType()!;
            if (indexType.IsPointer())
                indexType = typeof(IntPtr);

            Type[] parameterTypes =
            [
                generic == true ? declaringType!.IsValueType ? declaringType.MakeByRefType() : declaringType : Reflector.ObjectType, // instance
                generic == true ? valueParameterType : Reflector.ObjectType, // value
                generic switch // indices/index
                {
                    false => Reflector.ObjectType,
                    true => indexType,
                    null => typeof(object[])
                }
            ];

            var dm = new DynamicMethod("<SetRefIndexer>__" + Property.Name, Reflector.VoidType, parameterTypes, GetOwner(), true);

            ILGenerator il = dm.GetILGenerator();

            // generating locals for ByRef parameters (in C# indexers can only have the 'in' modifier)
            if (generic != true)
            {
                for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].IsByRef)
                        continue;

                    Type paramType = ParameterTypes[i].GetElementType()!;
                    il.DeclareLocal(paramType);

                    // initializing locals of ref (non-out) parameters
                    if (!Parameters[i].IsOut) // in C# this is always true
                    {
                        // from the object[] parameters
                        if (generic == null)
                        {
                            il.Emit(OpCodes.Ldarg_2); // loading parameters argument
                            il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                            il.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                        }
                        // from separate parameters
                        else
                            il.Emit(OpCodes.Ldarg_2); // loading the index argument - only single parameter indexers are supported this way

                        il.Emit(paramType.IsValueType || paramType.IsPointer() ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType.IsPointer() ? typeof(IntPtr) : paramType);
                        il.Emit(OpCodes.Stloc, localsIndex); // storing value in local variable
                    }

                    localsIndex++;
                }
            }

            // loading 0th argument (instance)
            il.Emit(OpCodes.Ldarg_0);
            if (generic != true)
                il.Emit(declaringType!.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);

            // assigning parameter(s)
            switch (generic)
            {
                case true:
                    if (ParameterTypes[0].IsByRef)
                        il.Emit(OpCodes.Ldarga, 2); // loading the address of the index parameter
                    else
                        il.Emit(OpCodes.Ldarg_2); // loading the index parameter
                    break;

                case false:
                    if (ParameterTypes[0].IsByRef)
                        il.Emit(OpCodes.Ldloca, 0); // passing the address of the local variable for the byref index
                    else
                    {
                        il.Emit(OpCodes.Ldarg_2);
                        il.Emit(indexType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, indexType);
                    }

                    break;

                default:
                    for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                    {
                        Type paramType = ParameterTypes[i];
                        if (paramType.IsByRef)
                        {
                            il.Emit(OpCodes.Ldloca, localsIndex++); // passing the address of the local variables for byref parameters
                            continue;
                        }

                        if (paramType.IsPointer())
                            paramType = typeof(IntPtr);
                        il.Emit(OpCodes.Ldarg_2); // loading 2nd argument (indices)
                        il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                        il.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                        il.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
                    }

                    break;
            }

            // calling the getter
            il.Emit(getterMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getterMethod);

            // Assigning back ref/out parameters (though in C# only 'in' parameters are allowed, for which this never applies)
            if (generic == null)
            {
                for (int i = 0, localsIndex = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].IsByRef || Parameters[i].IsIn && !Parameters[i].IsOut)
                        continue;

                    Type paramType = ParameterTypes[i].GetElementType()!;
                    il.Emit(OpCodes.Ldarg_2); // loading 2nd argument (indices)
                    il.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                    il.Emit(OpCodes.Ldloc, (short)localsIndex); // loading local variable
                    ++localsIndex;

                    if (paramType.IsValueType || paramType.IsPointer())
                        il.Emit(OpCodes.Box, paramType.IsPointer() ? typeof(IntPtr) : paramType); // boxing value type into object
                    il.Emit(OpCodes.Stelem_Ref); // storing the variable into the pointed array index
                }
            }

            // loading 1st argument (value)
            il.Emit(OpCodes.Ldarg_1);
            if (generic != true)
                il.Emit(valueParameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, valueParameterType);

            // setting the returned reference
            if (isPointer)
                il.Emit(OpCodes.Stind_I);
            else if (propertyType.IsValueType)
                il.Emit(OpCodes.Stobj, propertyType);
            else
                il.Emit(OpCodes.Stind_Ref);

            il.Emit(OpCodes.Ret);
            return dm;
        }
#endif

        #endregion

        #endregion
    }
}
