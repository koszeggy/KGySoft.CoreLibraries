#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FunctionMethodAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
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
            MethodInfo method = (MethodInfo)MemberInfo;
            Type? declaringType = method.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true || method.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!method.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

#if NETSTANDARD2_0
            if (method.ReturnType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(method.ReturnType));

            // Non-readonly value type or has ref/out/pointer parameters or pointer return type: using reflection as fallback so mutations are preserved and ref/out parameters are assigned back
            if (!method.IsStatic && declaringType!.IsValueType && !(declaringType.IsReadOnly() || method.IsReadOnly())
                || method.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.IsIn || p.IsOut) || p.ParameterType.IsPointer)
                || method.ReturnType.IsPointer)
            {
                ThrowIfNotSupportedParameters();
                unsafe
                {
                    return method.ReturnType.IsPointer
                        ? (instance, args) => (IntPtr)Pointer.Unbox(method.Invoke(instance, args))
                        : method.Invoke;
                }
            }

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "target");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var methodParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];
           
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
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, DynamicMethodOptions.None);
            return (Func<object?, object?[]?, object?>)dm.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericInvoker()
        {
            MethodInfo method = (MethodInfo)MemberInfo;
            Type? declaringType = method.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true || method.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!method.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (ParameterTypes.Length > 4)
                Throw.NotSupportedException(); // will be handled in PostValidate

            Type delegateType = ParameterTypes.Length switch
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
            bool isPointerReturn = method.ReturnType.IsPointer;
            if (!method.IsStatic && declaringType!.IsValueType && !(declaringType.IsReadOnly() || method.IsReadOnly())
                || ParameterTypes.Any(p => p.IsPointer) || isPointerReturn)
            {
                ThrowIfNotSupportedParameters();
                unsafe
                {
                    return ParameterTypes.Length switch
                    {
                        0 => (Func<object?, object?>)(isPointerReturn ? (o => (IntPtr)Pointer.Unbox(method.Invoke(o, null))) : (o => method.Invoke(o, null))),
                        1 => (Func<object?, object?, object?>)(isPointerReturn ? (o, p) => (IntPtr)Pointer.Unbox(method.Invoke(o, [p])) : (o, p) => method.Invoke(o, [p])),
                        2 => (Func<object?, object?, object?, object?>)(isPointerReturn ? (o, p1, p2) => (IntPtr)Pointer.Unbox(method.Invoke(o, [p1, p2])) : (o, p1, p2) => method.Invoke(o, [p1, p2])),
                        3 => (Func<object?, object?, object?, object?, object?>)(isPointerReturn ? (o, p1, p2, p3) => (IntPtr)Pointer.Unbox(method.Invoke(o, [p1, p2, p3])) : (o, p1, p2, p3) => method.Invoke(o, [p1, p2, p3])),
                        4 => (Func<object?, object?, object?, object?, object?, object?>)(isPointerReturn ? (o, p1, p2, p3, p4) => (IntPtr)Pointer.Unbox(method.Invoke(o, [p1, p2, p3, p4])) : (o, p1, p2, p3, p4) => method.Invoke(o, [p1, p2, p3, p4])),
                        _ => Throw.InternalError<Delegate>("Unexpected number of parameters")
                    };
                }
            }

            var parameters = new ParameterExpression[ParameterTypes.Length + 1];
            parameters[0] = Expression.Parameter(Reflector.ObjectType, "instance");
            var methodParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                parameters[i + 1] = Expression.Parameter(Reflector.ObjectType, $"param{i + 1}");
                Type parameterType = ParameterTypes[i];

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
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, DynamicMethodOptions.ExactParameters);
            return dm.CreateDelegate(delegateType);
#endif
        }

        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Expression.Call does not write the parameters")]
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
            if (ParameterTypes.Length > 4)
                Throw.NotSupportedException(Res.ReflectionMethodGenericNotSupported);

            bool isByRef = method.ReturnType.IsByRef;
            Type returnType = isByRef ? method.ReturnType.GetElementType()! : method.ReturnType;
            if (returnType.IsPointer)
                returnType = typeof(IntPtr);

            Type delegateType;
            if (isStatic)
            {
                delegateType = (ParameterTypes.Length switch
                {
                    0 => typeof(Func<>),
                    1 => typeof(Func<,>),
                    2 => typeof(Func<,,>),
                    3 => typeof(Func<,,,>),
                    4 => typeof(Func<,,,,>),
                    _ => Throw.InternalError<Type>("Unexpected number of parameters")
                }).GetGenericType(GetGenericArguments(ParameterTypes)
                    .Append(returnType)
                    .ToArray());
            }
            else
            {
                if (isValueType)
                {
                    delegateType = ParameterTypes.Length switch
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
                    delegateType = ParameterTypes.Length switch
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
                    .Concat(GetGenericArguments(ParameterTypes))
                    .Append(returnType)
                    .ToArray());
            }

#if NETSTANDARD2_0
            if (isByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(method.ReturnType));

            ParameterExpression[] parameters;
            Expression[] methodParameters;
            MethodCallExpression methodCall;
            LambdaExpression lambda;

            // Method has a pointer parameter: fallback to MethodInfo.Invoke(object,object[]), which supports pointers as IntPtr...
            if (ParameterTypes.Any(p => p.IsPointer || method.ReturnType.IsPointer))
            {
                ThrowIfNotSupportedParameters(); // ...except ref pointers

                // value types: though we can call Invoke(object, object[]), the ref instance parameter gets boxed in a new object, losing all mutations
                if (isValueType && !isStatic && !declaringType!.IsReadOnly() && !method.IsReadOnly())
                    Throw.PlatformNotSupportedException(Res.ReflectionValueTypeWithPointersGenericNetStandard20);
                int offset = isStatic ? 0 : 1;
                parameters = new ParameterExpression[ParameterTypes.Length + offset];
                if (!isStatic)
                    parameters[0] = Expression.Parameter(isValueType ? declaringType!.MakeByRefType() : declaringType!, "instance");

                Type[] genericArgs = delegateType.GetGenericArguments();
                for (int i = offset; i < parameters.Length; i++)
                    parameters[i] = Expression.Parameter(genericArgs[i], $"param{i + 1}");

                methodParameters = new Expression[2];
                methodParameters[0] = isStatic
                    ? Expression.Constant(null, typeof(object))
                    : Expression.Convert(parameters[0], typeof(object));

                methodParameters[1] = Expression.NewArrayInit(typeof(object), parameters.Skip(isStatic ? 0 : 1).Select(p => Expression.Convert(p, typeof(object))));

                // NOTE: If the return type is pointer, we should call Pointer.Unbox on the MethodInfo.Invoke result, which is not possible by expression trees.
                // So we use the GeneralInvoker delegate for pointer return types, whose Invoke has the same signature as MethodInfo.Invoke(object, object[]),
                // and it converts the pointer result to IntPtr.
                object callTarget = method.ReturnType.IsPointer ? GeneralInvoker : method;
                methodCall = Expression.Call(
                    Expression.Constant(callTarget),
                    callTarget.GetType().GetMethod(nameof(MethodInfo.Invoke), [typeof(object), typeof(object[])])!,
                    methodParameters);

                lambda = Expression.Lambda(delegateType, Expression.Convert(methodCall, returnType), parameters);
                return lambda.Compile();
            }

            // Static methods
            if (isStatic)
            {
                parameters = new ParameterExpression[ParameterTypes.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = ParameterTypes[i];

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
            parameters = new ParameterExpression[ParameterTypes.Length + 1];
            methodParameters = new Expression[ParameterTypes.Length];

            if (!isValueType)
                parameters[0] = Expression.Parameter(declaringType!, "instance");
            else
                parameters[0] = Expression.Parameter(declaringType!.MakeByRefType(), "instance");

            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];
                Type methodParameterType = parameterType.IsByRef ? parameterType.GetElementType()! // This just avoids error when ref parameters are used but does not assign results back
                    : parameterType.IsPointer ? typeof(IntPtr)
                    : parameterType;

                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                parameters[i + 1] = Expression.Parameter(methodParameterType, $"param{i + 1}");
                methodParameters[i] = parameterType.IsPointer ? Expression.Convert(parameters[i + 1], methodParameterType) : parameters[i + 1];
            }

            methodCall = Expression.Call(parameters[0], method, methodParameters);
            lambda = Expression.Lambda(delegateType, methodCall, parameters);
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return dm.CreateDelegate(delegateType);
#endif
        }

        #endregion
    }
}
