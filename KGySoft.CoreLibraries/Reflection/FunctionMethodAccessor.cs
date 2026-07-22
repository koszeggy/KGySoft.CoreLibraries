#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FunctionMethodAccessor.cs
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
    /// <summary>
    /// Function method accessor for any parameters.
    /// </summary>
    internal sealed class FunctionMethodAccessor : MethodAccessor
    {
        #region Constructors

        internal FunctionMethodAccessor(MethodInfo mi)
            : base(mi)
        {
        }

        #endregion

        #region Methods

        private protected override Func<object?, object?[]?, object?> CreateGeneralInvoker()
        {
            #region Local Methods

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Func<object?, object?[]?, object?> SystemReflectionFallback()
            {
                MethodInfo mi = (MethodInfo)MemberInfo;
                Type returnType = mi.ReturnType;
                if (returnType.IsByRef)
                    returnType = returnType.GetElementType()!;

                unsafe
                {
                    // Only real pointers are returned as Reflection.Pointer, whereas function pointers are returned as IntPtr,
                    // so using the IsPointer property rather than the IsPointer() extension here is intended.
#if NET8_0_OR_GREATER
                    MethodInvoker invoker = FallbackInvoker;
                    return returnType.IsPointer ? (obj, args) => (IntPtr)Pointer.Unbox(invoker.Invoke(obj, args.AsSpan())!) : (obj, args) => invoker.Invoke(obj, args.AsSpan());
#else
                    return returnType.IsPointer ? (instance, args) => (IntPtr)Pointer.Unbox(mi.Invoke(instance, args)!) : mi.Invoke;
#endif
                }
            }
#endif

            #endregion

            MethodInfo method = (MethodInfo)MemberInfo;
            Type? declaringType = method.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true || method.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!method.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

#if NETSTANDARD2_0
            ThrowIfHasRefPointerParameters();
            if (method.ReturnType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(method.ReturnType));

            // Non-readonly value type or has ref/out/pointer parameters or pointer return type: using reflection as fallback so mutations are preserved and ref/out parameters are assigned back
            if (!method.IsStatic && declaringType!.IsValueType && !(declaringType.IsReadOnly() || method.IsReadOnly())
                || method.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.IsIn || p.IsOut) || p.ParameterType.IsPointer()) || method.ReturnType.IsPointer())
            {
                return SystemReflectionFallback();
            }

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "target");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var methodParameters = new Expression[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                Type parameterType = Parameters[i].ParameterType;
           
                // for in parameters
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                // ReSharper disable once AssignNullToNotNullAttribute
                methodParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            MethodCallExpression methodToCall = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                method, // method info
                methodParameters); // arguments cast to target types

            var lambda = Expression.Lambda<Func<object?, object?[]?, object?>>(
                Expression.Convert(methodToCall, Reflector.ObjectType), // return type converted to object
                instanceParameter, // instance (object)
                argumentsParameter);
            return lambda.Compile();
#else
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                ThrowIfHasRefPointerParameters();
                return SystemReflectionFallback();
            }
#endif
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, DynamicMethodOptions.None);
            return (Func<object?, object?[]?, object?>)dm.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericInvoker()
        {
            #region Local Methods

#if NET8_0_OR_GREATER
            unsafe Delegate SystemReflectionFallback()
            {
                MethodInfo mi = (MethodInfo)Method;
                Type returnType = mi.ReturnType;
                if (returnType.IsByRef)
                    returnType = returnType.GetElementType()!;

                // Only real pointers are returned as Reflection.Pointer, whereas function pointers are returned as IntPtr,
                // so using the IsPointer property rather than the IsPointer() extension here is intended.
                bool isPointerReturn = returnType.IsPointer;
                MethodInvoker invoker = FallbackInvoker;
                return Parameters.Length switch
                {
                    0 => isPointerReturn ? o => (IntPtr)Pointer.Unbox(invoker.Invoke(o)!) : new Func<object?, object?>(invoker.Invoke),
                    1 => isPointerReturn ? (o, p) => (IntPtr)Pointer.Unbox(invoker.Invoke(o, p)!) : new Func<object?, object?, object?>(invoker.Invoke),
                    2 => isPointerReturn ? (o, p1, p2) => (IntPtr)Pointer.Unbox(invoker.Invoke(o, p1, p2)!) : new Func<object?, object?, object?, object?>(invoker.Invoke),
                    3 => isPointerReturn ? (o, p1, p2, p3) => (IntPtr)Pointer.Unbox(invoker.Invoke(o, p1, p2, p3)!) : new Func<object?, object?, object?, object?, object?>(invoker.Invoke),
                    4 => isPointerReturn ? (o, p1, p2, p3, p4) => (IntPtr)Pointer.Unbox(invoker.Invoke(o, p1, p2, p3, p4)!) : new Func<object?, object?, object?, object?, object?, object?>(invoker.Invoke),
                    _ => Throw.InternalError<Delegate>("Unexpected number of parameters")
                };
            }
#elif NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            unsafe Delegate SystemReflectionFallback()
            {
                MethodInfo mi = (MethodInfo)Method;
                Type returnType = mi.ReturnType;
                if (returnType.IsByRef)
                    returnType = returnType.GetElementType()!;

                // Only real pointers are returned as Reflection.Pointer, whereas function pointers are returned as IntPtr,
                // so using the IsPointer property rather than the IsPointer() extension here is intended.
                bool isPointerReturn = returnType.IsPointer;
                return Parameters.Length switch
                {
                    0 => (Func<object?, object?>)(isPointerReturn ? o => (IntPtr)Pointer.Unbox(mi.Invoke(o, null)!) : (o => mi.Invoke(o, null))),
                    1 => (Func<object?, object?, object?>)(isPointerReturn ? (o, p) => (IntPtr)Pointer.Unbox(mi.Invoke(o, [p])!) : (o, p) => mi.Invoke(o, [p])),
                    2 => (Func<object?, object?, object?, object?>)(isPointerReturn ? (o, p1, p2) => (IntPtr)Pointer.Unbox(mi.Invoke(o, [p1, p2])!) : (o, p1, p2) => mi.Invoke(o, [p1, p2])),
                    3 => (Func<object?, object?, object?, object?, object?>)(isPointerReturn ? (o, p1, p2, p3) => (IntPtr)Pointer.Unbox(mi.Invoke(o, [p1, p2, p3])!) : (o, p1, p2, p3) => mi.Invoke(o, [p1, p2, p3])),
                    4 => (Func<object?, object?, object?, object?, object?, object?>)(isPointerReturn ? (o, p1, p2, p3, p4) => (IntPtr)Pointer.Unbox(mi.Invoke(o, [p1, p2, p3, p4])!) : (o, p1, p2, p3, p4) => mi.Invoke(o, [p1, p2, p3, p4])),
                    _ => Throw.InternalError<Delegate>("Unexpected number of parameters")
                };
            }
#endif

            #endregion

            MethodInfo method = (MethodInfo)MemberInfo;
            Type? declaringType = method.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true || method.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!method.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Parameters.Length > 4)
                Throw.NotSupportedException(); // will be handled in PostValidate

            Type delegateType = Parameters.Length switch
            {
                0 => typeof(Func<object?, object?>),
                1 => typeof(Func<object?, object?, object?>),
                2 => typeof(Func<object?, object?, object?, object?>),
                3 => typeof(Func<object?, object?, object?, object?, object?>),
                4 => typeof(Func<object?, object?, object?, object?, object?, object?>),
                _ => Throw.InternalError<Type>("Unexpected number of parameters")
            };

#if NETSTANDARD2_0
            if (method.ReturnType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(method.ReturnType));

            // For non-readonly value types using reflection as fallback so mutations are preserved. Likewise, defaulting to reflection if pointer return type or parameters are used.
            bool isPointerReturn = method.ReturnType.IsPointer();
            ThrowIfHasRefPointerParameters();
            if (!method.IsStatic && declaringType!.IsValueType && !(declaringType.IsReadOnly() || method.IsReadOnly()) || Parameters.Any(p => p.ParameterType.IsPointer()) || isPointerReturn)
                return SystemReflectionFallback();

            var parameters = new ParameterExpression[Parameters.Length + 1];
            parameters[0] = Expression.Parameter(Reflector.ObjectType, "instance");
            var methodParameters = new Expression[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
            {
                parameters[i + 1] = Expression.Parameter(Reflector.ObjectType, $"param{i + 1}");
                Type parameterType = Parameters[i].ParameterType;

                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                methodParameters[i] = Expression.Convert(parameters[i + 1], parameterType);
            }

            MethodCallExpression methodToCall = Expression.Call(
                method.IsStatic ? null : Expression.Convert(parameters[0], declaringType!), // (TInstance)instance
                method, // method info
                methodParameters); // parameters cast to target types

            var lambda = Expression.Lambda(delegateType,
                Expression.Convert(methodToCall, Reflector.ObjectType), // return type converted to object
                parameters);
            return lambda.Compile();
#else
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                ThrowIfHasRefPointerParameters();
                return SystemReflectionFallback();
            }
#endif
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, DynamicMethodOptions.ExactParameters);
            return dm.CreateDelegate(delegateType);
#endif
        }

        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Expression.Call does not write the parameters")]
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Many simple switches for the generic delegate types.")]
        private protected override Delegate CreateGenericInvoker()
        {
            var method = (MethodInfo)Method;
            Type? declaringType = Method.DeclaringType;
            bool isStatic = method.IsStatic;
            bool isValueType = declaringType?.IsValueType == true;
            if (declaringType?.ContainsGenericParameters == true || method.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (isStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Parameters.Length > 4)
                Throw.NotSupportedException(Res.ReflectionMethodGenericNotSupported);

            bool isByRef = method.ReturnType.IsByRef;
            Type returnType = isByRef ? method.ReturnType.GetElementType()! : method.ReturnType;
            bool isPointerReturn = returnType.IsPointer();
            if (isPointerReturn)
                returnType = typeof(IntPtr);

            Type delegateType;
            if (isStatic)
            {
                delegateType = (Parameters.Length switch
                {
                    0 => typeof(Func<>),
                    1 => typeof(Func<,>),
                    2 => typeof(Func<,,>),
                    3 => typeof(Func<,,,>),
                    4 => typeof(Func<,,,,>),
                    _ => Throw.InternalError<Type>("Unexpected number of parameters")
                }).GetGenericType(GetGenericArguments(Parameters.Select(p => p.ParameterType))
                    .Append(returnType)
                    .ToArray());
            }
            else
            {
                if (isValueType)
                {
                    delegateType = Parameters.Length switch
                    {
                        0 => typeof(ValueTypeFunction<,>),
                        1 => typeof(ValueTypeFunction<,,>),
                        2 => typeof(ValueTypeFunction<,,,>),
                        3 => typeof(ValueTypeFunction<,,,,>),
                        4 => typeof(ValueTypeFunction<,,,,,>),
                        _ => Throw.InternalError<Type>("Unexpected number of parameters")
                    };
                }
                else
                {
                    delegateType = Parameters.Length switch
                    {
                        // NOTE: actually we could use simple Func but that would make possible to invoke an instance method by a static invoker
                        0 => typeof(ReferenceTypeFunction<,>),
                        1 => typeof(ReferenceTypeFunction<,,>),
                        2 => typeof(ReferenceTypeFunction<,,,>),
                        3 => typeof(ReferenceTypeFunction<,,,,>),
                        4 => typeof(ReferenceTypeFunction<,,,,,>),
                        _ => Throw.InternalError<Type>("Unexpected number of parameters")
                    };
                }

                delegateType = delegateType.GetGenericType(new[] { declaringType! }
                    .Concat(GetGenericArguments(Parameters.Select(p => p.ParameterType)))
                    .Append(returnType)
                    .ToArray());
            }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#if NETSTANDARD2_0
            if (isByRef) // not even the fallback supports ref returns below .NET Core 3.0
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(method.ReturnType));
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
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return dm.CreateDelegate(delegateType);
#endif

            #region Local Methods

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Delegate CreateByExpressions()
            {
                ParameterExpression[] parameters;
                Expression[] methodParameters;
                MethodCallExpression methodCall;
                LambdaExpression lambda;
                ThrowIfHasRefPointerParameters();

                // Method has a pointer parameter, or the return type is pointer or ref: fallback to System reflection, which supports pointers as IntPtr.
                if (Parameters.Any(p => p.ParameterType.IsPointer()) || isPointerReturn || isByRef)
                {

                    // value types: though we can call Invoke(object, object[]), the ref instance parameter gets boxed in a new object, losing all mutations
                    if (isValueType && !isStatic && !declaringType!.IsReadOnly() && !method.IsReadOnly())
                        ThrowMutableStructMembersNotSupported();

                    int offset = isStatic ? 0 : 1;
                    parameters = new ParameterExpression[Parameters.Length + offset];
                    if (!isStatic)
                        parameters[0] = Expression.Parameter(isValueType ? declaringType!.MakeByRefType() : declaringType!, "instance");

                    Type[] genericArgs = delegateType.GetGenericArguments();
                    for (int i = offset; i < parameters.Length; i++)
                        parameters[i] = Expression.Parameter(genericArgs[i], $"param{i + 1 - offset}");

#if NET8_0_OR_GREATER
                    // fallback to MethodInvoker
                    MethodInvoker invoker = FallbackInvoker;
                    methodParameters = new Expression[(isStatic ? 1 : 0) + parameters.Length];
                    methodParameters[0] = isStatic ? Expression.Constant(null, typeof(object))
                        : parameters[0].Type == typeof(object) ? parameters[0]
                        : Expression.Convert(parameters[0], typeof(object));

                    for (int i = 0; i < Parameters.Length; i++)
                        methodParameters[i + 1] = parameters[i + offset].Type == typeof(object) ? parameters[i + offset] : Expression.Convert(parameters[i + offset], typeof(object));

                    // NOTE: If the return type is pointer, we should call Pointer.Unbox on the MethodInvoker.Invoke result, which is not possible by expression trees.
                    // So we use the NonGenericInvoker delegate for pointer return types, whose Invoke has the same signature as NonGenericInvoker.Invoke(object[, ...]),
                    // and it converts the pointer result to IntPtr.
                    object callTarget = isPointerReturn ? NonGenericInvoker : invoker;
                    methodCall = Expression.Call(
                        Expression.Constant(callTarget), // the instance is the MethodInvoker or the already generated NonGenericInvoker delegate instance
                        Parameters.Length switch
                        {
                            0 => callTarget.GetType().GetMethod(nameof(MethodInvoker.Invoke), [typeof(object)])!, // no pointer parameters in this case, but ref return is possible
                            1 => callTarget.GetType().GetMethod(nameof(MethodInvoker.Invoke), [typeof(object), typeof(object)])!,
                            2 => callTarget.GetType().GetMethod(nameof(MethodInvoker.Invoke), [typeof(object), typeof(object), typeof(object)])!,
                            3 => callTarget.GetType().GetMethod(nameof(MethodInvoker.Invoke), [typeof(object), typeof(object), typeof(object), typeof(object)])!,
                            4 => callTarget.GetType().GetMethod(nameof(MethodInvoker.Invoke), [typeof(object), typeof(object), typeof(object), typeof(object), typeof(object)])!,
                            _ => throw new InvalidOperationException(Res.InternalError("Unexpected number of parameters"))
                        },
                        methodParameters);
#else
                    // fallback to MethodInfo.Invoke(object,object[])
                    methodParameters = new Expression[2];
                    methodParameters[0] = isStatic ? Expression.Constant(null, typeof(object))
                        : parameters[0].Type == typeof(object) ? parameters[0]
                        : Expression.Convert(parameters[0], typeof(object));

                    methodParameters[1] = Expression.NewArrayInit(typeof(object), parameters.Skip(isStatic ? 0 : 1).Select(p => Expression.Convert(p, typeof(object))));

                    // NOTE: If the return type is pointer, we should call Pointer.Unbox on the MethodInfo.Invoke result, which is not possible by expression trees.
                    // So we use the GeneralInvoker delegate for pointer return types, whose Invoke has the same signature as MethodInfo.Invoke(object, object[]),
                    // and it converts the pointer result to IntPtr.
                    object callTarget = isPointerReturn ? GeneralInvoker : method;
                    methodCall = Expression.Call(
                        Expression.Constant(callTarget),
                        callTarget.GetType().GetMethod(nameof(MethodInfo.Invoke), [typeof(object), typeof(object[])])!,
                        methodParameters);
#endif

                    lambda = Expression.Lambda(delegateType, returnType == typeof(object) ? methodCall : Expression.Convert(methodCall, returnType), parameters);
                    return lambda.Compile();
                }

                // Static methods
                if (isStatic)
                {
                    parameters = new ParameterExpression[Parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        Type parameterType = Parameters[i].ParameterType;

                        // This just avoids error when ref parameters are used but does not assign results back
                        if (parameterType.IsByRef)
                            parameterType = parameterType.GetElementType()!;

                        parameters[i] = Expression.Parameter(parameterType, $"param{i + 1}");
                    }

                    methodCall = Expression.Call(null, method, parameters);

                    lambda = Expression.Lambda(delegateType, methodCall, parameters);
                    return lambda.Compile();
                }

                // Instance methods
                parameters = new ParameterExpression[Parameters.Length + 1];
                methodParameters = new Expression[Parameters.Length];

                if (!isValueType)
                    parameters[0] = Expression.Parameter(declaringType!, "instance");
                else
                    parameters[0] = Expression.Parameter(declaringType!.MakeByRefType(), "instance");

                for (int i = 0; i < Parameters.Length; i++)
                {
                    Type parameterType = Parameters[i].ParameterType;
                    Type methodParameterType = parameterType.IsByRef ? parameterType.GetElementType()! // This just avoids error when ref parameters are used but does not assign results back
                        : parameterType.IsPointer() ? typeof(IntPtr)
                        : parameterType;

                    // This just avoids error when ref parameters are used but does not assign results back
                    if (parameterType.IsByRef)
                        parameterType = parameterType.GetElementType()!;

                    parameters[i + 1] = Expression.Parameter(methodParameterType, $"param{i + 1}");
                    methodParameters[i] = parameterType.IsPointer() ? Expression.Convert(parameters[i + 1], methodParameterType) : parameters[i + 1];
                }

                methodCall = Expression.Call(parameters[0], method, methodParameters);
                lambda = Expression.Lambda(delegateType, methodCall, parameters);
                return lambda.Compile();
            }
#endif

            #endregion
        }

        #endregion
    }
}
