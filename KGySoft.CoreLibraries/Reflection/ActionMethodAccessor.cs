﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ActionMethodAccessor.cs
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
    /// Action method accessor invoker for any parameters.
    /// </summary>
    internal sealed class ActionMethodAccessor : MethodAccessor
    {
        #region Constructors

        internal ActionMethodAccessor(MethodBase mi) // Now can be used for ctors but that is not cached in the base! See Accessors.GetCtorMethod
            : base(mi)
        {
        }

        #endregion

        #region Methods

        private protected override Func<object?, object?[]?, object?> CreateGeneralInvoker()
        {
            var methodBase = (MethodBase)MemberInfo;
            Type? declaringType = methodBase.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true || methodBase.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!methodBase.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

#if NETSTANDARD2_0
            // Constructor, non-readonly value type or has ref/out/pointer parameters: using reflection as fallback so
            // - Constructor can be executed as a method
            // - Mutations are preserved
            // - ref/out parameters are assigned back
            // - IntPtr values are accepted for pointer parameters
            if (methodBase is not MethodInfo method
                || !method.IsStatic && declaringType!.IsValueType && !(declaringType.IsReadOnly() || method.IsReadOnly())
                || methodBase.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.IsIn || p.IsOut) || p.ParameterType.IsPointer))
            {
                ThrowIfNotSupportedParameters();
                return methodBase.Invoke;
            }

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var methodParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];

                // for in parameters
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                methodParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            // ReSharper disable once AssignNullToNotNullAttribute - declaring type was already checked above
            MethodCallExpression methodToCall = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                method, // method info
                methodParameters); // parameters cast to target types

            BlockExpression body = Expression.Block(
                methodToCall, // the void method call
                Expression.Constant(null)); // return null
            var lambda = Expression.Lambda<Func<object?, object?[]?, object?>>(
                body, // void method call + return null
                instanceParameter, // instance (object)
                argumentsParameter);
            return lambda.Compile();
#else
            var options = DynamicMethodOptions.ReturnNullForVoid;
            if (methodBase is ConstructorInfo)
                options |= DynamicMethodOptions.TreatCtorAsMethod;
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(methodBase, options);
            return (Func<object?, object?[]?, object?>)dm.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericInvoker()
        {
            if (Method is not MethodInfo method)
                return Throw.InternalError<Delegate>($"Constructor {Method} is not expected in {nameof(CreateNonGenericInvoker)}");
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
            // For non-readonly value types using reflection as fallback so mutations are preserved. Likewise, defaulting to reflection if pointer parameters are used.
            if (!method.IsStatic && declaringType!.IsValueType && !(declaringType.IsReadOnly() || method.IsReadOnly())
                || ParameterTypes.Any(p => p.IsPointer))
            {
                ThrowIfNotSupportedParameters();
                return ParameterTypes.Length switch
                {
                    0 => new Func<object?, object?>(o => method.Invoke(o, null)),
                    1 => new Func<object?, object?, object?>((o, p) => method.Invoke(o, [p])),
                    2 => new Func<object?, object?, object?, object?>((o, p1, p2) => method.Invoke(o, [p1, p2])),
                    3 => new Func<object?, object?, object?, object?, object?>((o, p1, p2, p3) => method.Invoke(o, [p1, p2, p3])),
                    4 => new Func<object?, object?, object?, object?, object?, object?>((o, p1, p2, p3, p4) => method.Invoke(o, [p1, p2, p3, p4])),
                    _ => Throw.InternalError<Delegate>("Unexpected number of parameters")
                };
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

            BlockExpression body = Expression.Block(
                methodToCall, // the void method call
                Expression.Constant(null)); // return null
            var lambda = Expression.Lambda(delegateType,
                body, // void method call + return null
                parameters);
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.ReturnNullForVoid);
            return dm.CreateDelegate(delegateType);
#endif
        }

        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Expression.Call does not write the parameters")]
        private protected override Delegate CreateGenericInvoker()
        {
            if (Method is not MethodInfo method)
                return Throw.InternalError<Delegate>($"Constructor {Method} is not expected in {nameof(CreateGenericInvoker)}");

            Type? declaringType = method.DeclaringType;
            bool isStatic = method.IsStatic;
            bool isValueType = declaringType?.IsValueType == true;
            if (declaringType?.ContainsGenericParameters == true || method.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!isStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (ParameterTypes.Length > 4)
                Throw.NotSupportedException(Res.ReflectionMethodGenericNotSupported);

            Type delegateType;
            if (isStatic)
            {
                delegateType = ParameterTypes.Length switch
                {
                    0 => typeof(Action),
                    1 => typeof(Action<>),
                    2 => typeof(Action<,>),
                    3 => typeof(Action<,,>),
                    4 => typeof(Action<,,,>),
                    _ => Throw.InternalError<Type>("Unexpected number of parameters")
                };

                if (delegateType.IsGenericTypeDefinition)
                    delegateType = delegateType.GetGenericType(GetGenericArguments(ParameterTypes).ToArray());
            }
            else
            {
                if (isValueType)
                {
                    delegateType = ParameterTypes.Length switch
                    {
                        0 => typeof(ValueTypeAction<>),
                        1 => typeof(ValueTypeAction<,>),
                        2 => typeof(ValueTypeAction<,,>),
                        3 => typeof(ValueTypeAction<,,,>),
                        4 => typeof(ValueTypeAction<,,,,>),
                        _ => Throw.InternalError<Type>("Unexpected number of parameters")
                    };
                }
                else
                {
                    delegateType = ParameterTypes.Length switch
                    {
                        // NOTE: actually we could use simple Action but that would make possible to invoke an instance method by a static invoker
                        0 => typeof(ReferenceTypeAction<>),
                        1 => typeof(ReferenceTypeAction<,>),
                        2 => typeof(ReferenceTypeAction<,,>),
                        3 => typeof(ReferenceTypeAction<,,,>),
                        4 => typeof(ReferenceTypeAction<,,,,>),
                        _ => Throw.InternalError<Type>("Unexpected number of parameters")
                    };
                }

                delegateType = delegateType.GetGenericType(new[] { declaringType! }
                    .Concat(GetGenericArguments(ParameterTypes))
                    .ToArray());
            }

#if NETSTANDARD2_0
            ParameterExpression[] parameters;
            Expression[] methodParameters;
            MethodCallExpression methodCall;
            LambdaExpression lambda;

            // Method has a pointer parameter: fallback to MethodInfo.Invoke(object,object[]), which supports pointer parameters as IntPtr...
            if (ParameterTypes.Any(p => p.IsPointer))
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
                methodCall = Expression.Call(
                    Expression.Constant(method), // the instance is the MethodInfo itself
                    method.GetType().GetMethod(nameof(MethodInfo.Invoke), [typeof(object), typeof(object[])])!, // Invoke(object, object[])
                    methodParameters);

                lambda = Expression.Lambda(delegateType, methodCall, parameters);
                return lambda.Compile();
            }

            // Static methods
            if (method.IsStatic)
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
                Type methodParameterType = parameterType.IsByRef
                    ? parameterType.GetElementType()! // This just avoids error when ref parameters are used but does not assign results back
                    : parameterType;
                
                parameters[i + 1] = Expression.Parameter(methodParameterType, $"param{i + 1}");
                methodParameters[i] = parameters[i + 1];
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
