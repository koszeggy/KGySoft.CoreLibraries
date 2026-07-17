#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParameterizedCreateInstanceAccessor.cs
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
    /// Object factory for creating new instance of an object via a specified constructor.
    /// </summary>
    internal sealed class ParameterizedCreateInstanceAccessor : CreateInstanceAccessor
    {
        #region Properties

#if NET8_0_OR_GREATER
        // Used in AOT mode where the faster dynamic methods cannot be used. It is still supposed to be faster than classic reflection by ConstructorInfo.
        private ConstructorInvoker FallbackInvoker => field ??= ConstructorInvoker.Create((ConstructorInfo)MemberInfo);
#endif

        #endregion

        #region Constructors

        internal ParameterizedCreateInstanceAccessor(ConstructorInfo ctor)
            : base(ctor)
        {
        }

        #endregion

        #region Methods

        private protected override Func<object?[]?, object> CreateGeneralInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            if (ctor.IsStatic)
                Throw.InvalidOperationException(Res.ReflectionInstanceCtorExpected);
            if (ctor.DeclaringType is { IsAbstract: true } or { ContainsGenericParameters: true })
                Throw.InvalidOperationException(Res.ReflectionCannotCreateInstanceOfType(ctor.DeclaringType));

#if NETSTANDARD2_0
            // Has ref/out parameters: using reflection as fallback so they will be assigned back.
            // Doing so also for pointers, as they are not supported by Expression trees.
            if (ctor.GetParameters().Any(p => p.ParameterType.IsByRef && (!p.IsIn || p.IsOut) || p.ParameterType.IsPointer))
            {
                ThrowIfHasRefPointerParameters();
                return ctor.Invoke;
            }

            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var ctorParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];

                // for in parameters
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                ctorParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            NewExpression construct = Expression.New(
                ctor, // constructor info
                ctorParameters); // arguments cast to target types

            var lambda = Expression.Lambda<Func<object?[]?, object>>(
                Expression.Convert(construct, Reflector.ObjectType), // return type converted to object
                argumentsParameter);
            return lambda.Compile();
#else
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            if (!RuntimeFeature.IsDynamicCodeSupported)
            {
                ThrowIfHasRefPointerParameters();
#if NET8_0_OR_GREATER
                ConstructorInvoker invoker = FallbackInvoker;
                return args => invoker.Invoke(args.AsSpan());
#else
                return ctor.Invoke;
#endif
            }
#endif
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.None);
            return (Func<object?[]?, object>)dm.CreateDelegate(typeof(Func<object?[]?, object>));
#endif
        }

        private protected override Delegate CreateNonGenericInitializer()
        {
            #region Local Methods

#if NET8_0_OR_GREATER
            Delegate SystemReflectionFallback()
            {
                ConstructorInvoker invoker = FallbackInvoker;
                return ParameterTypes.Length switch
                {
                    0 => new Func<object?>(invoker.Invoke),
                    1 => new Func<object?, object?>(invoker.Invoke),
                    2 => new Func<object?, object?, object?>(invoker.Invoke),
                    3 => new Func<object?, object?, object?, object?>(invoker.Invoke),
                    4 => new Func<object?, object?, object?, object?, object?>(invoker.Invoke),
                    _ => Throw.InternalError<Delegate>("Unexpected number of parameters")
                };
            }
#elif NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Delegate SystemReflectionFallback()
            {
                ConstructorInfo ci = (ConstructorInfo)MemberInfo;
                return ParameterTypes.Length switch
                {
                    0 => new Func<object?>(() => ci.Invoke(null)),
                    1 => new Func<object?, object?>(p => ci.Invoke([p])),
                    2 => new Func<object?, object?, object?>((p1, p2) => ci.Invoke([p1, p2])),
                    3 => new Func<object?, object?, object?, object?>((p1, p2, p3) => ci.Invoke([p1, p2, p3])),
                    4 => new Func<object?, object?, object?, object?, object?>((p1, p2, p3, p4) => ci.Invoke([p1, p2, p3, p4])),
                    _ => Throw.InternalError<Delegate>("Unexpected number of parameters")
                };
            }
#endif

            #endregion

            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            if (ctor.IsStatic)
                Throw.InvalidOperationException(Res.ReflectionInstanceCtorExpected);
            if (ctor.DeclaringType is { IsAbstract: true } or { ContainsGenericParameters: true })
                Throw.InvalidOperationException(Res.ReflectionCannotCreateInstanceOfType(ctor.DeclaringType!));
            if (ParameterTypes.Length > 4)
                Throw.NotSupportedException(); // will be handled in PostValidate

            Type delegateType = ParameterTypes.Length switch
            {
                0 => typeof(Func<object?>),
                1 => typeof(Func<object?, object?>),
                2 => typeof(Func<object?, object?, object?>),
                3 => typeof(Func<object?, object?, object?, object?>),
                4 => typeof(Func<object?, object?, object?, object?, object?>),
                _ => Throw.InternalError<Type>("Unexpected number of parameters")
            };

#if NETSTANDARD2_0
            // For pointer parameter types using reflection as fallback because Expression trees do not support pointers.
            if (ParameterTypes.Any(p => p.IsPointer))
            {
                ThrowIfHasRefPointerParameters();
                return SystemReflectionFallback();
            }

            var parameters = new ParameterExpression[ParameterTypes.Length];
            var ctorParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                parameters[i] = Expression.Parameter(Reflector.ObjectType, $"param{i + 1}");
                Type parameterType = ParameterTypes[i];

                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                ctorParameters[i] = Expression.Convert(parameters[i], parameterType);
            }

            NewExpression construct = Expression.New(
                ctor, // constructor info
                ctorParameters); // arguments cast to target types

            var lambda = Expression.Lambda(delegateType,
                Expression.Convert(construct, Reflector.ObjectType), // return type converted to object
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
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.ExactParameters);
            return dm.CreateDelegate(delegateType);
#endif
        }

        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Expression.New does not write the parameters")]
        private protected override Delegate CreateGenericInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            if (ctor.IsStatic)
                Throw.InvalidOperationException(Res.ReflectionInstanceCtorExpected);
            if (ctor.DeclaringType is { IsAbstract: true } or { ContainsGenericParameters: true })
                Throw.InvalidOperationException(Res.ReflectionCannotCreateInstanceOfType(ctor.DeclaringType!));
            if (ParameterTypes.Length > 4)
                Throw.NotSupportedException(Res.ReflectionCtorGenericNotSupported);

            Type delegateType = (ParameterTypes.Length switch
            {
                0 => typeof(Func<>),
                1 => typeof(Func<,>),
                2 => typeof(Func<,,>),
                3 => typeof(Func<,,,>),
                4 => typeof(Func<,,,,>),
                _ => Throw.InternalError<Type>("Unexpected number of parameters")
            }).GetGenericType(GetGenericArguments(ParameterTypes).Append(ctor.DeclaringType!).ToArray());

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
#if !NETSTANDARD2_0
            // Dynamic methods and IL generation are not supported: fallback to Expressions.
            // In AOT mode it will work in interpreted mode, which is even slower than the non-generic alternative...
            if (!RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                ParameterExpression[] parameters = new ParameterExpression[ParameterTypes.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = ParameterTypes[i];

                    // This just avoids error when ref parameters are used but does not assign results back
                    if (parameterType.IsByRef)
                        parameterType = parameterType.GetElementType()!;
                    if (parameterType.IsPointer)
                        parameterType = typeof(IntPtr);

                    parameters[i] = Expression.Parameter(parameterType, $"param{i + 1}");
                }

                LambdaExpression lambda;

                // The constructor has pointer parameters: fallback to System reflection, which supports pointer parameters as IntPtr...
                if (ParameterTypes.Any(p => p.IsPointer))
                {
                    ThrowIfHasRefPointerParameters(); // ...except ref pointers

#if NET8_0_OR_GREATER
                    // fallback to ConstructorInvoker
                    ConstructorInvoker invoker = FallbackInvoker;
                    Expression[] methodParameters = new Expression[parameters.Length];
                    for (int i = 0; i < parameters.Length; i++)
                        methodParameters[i] = parameters[i].Type == typeof(object) ? parameters[i] : Expression.Convert(parameters[i], typeof(object));

                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(invoker), // the instance is the ConstructorInvoker
                        methodParameters.Length switch
                        {
                            //0 => invoker.GetType().GetMethod(nameof(ConstructorInvoker.Invoke), [])!, // no pointer parameters in this case
                            1 => invoker.GetType().GetMethod(nameof(ConstructorInvoker.Invoke), [typeof(object)])!,
                            2 => invoker.GetType().GetMethod(nameof(ConstructorInvoker.Invoke), [typeof(object), typeof(object)])!,
                            3 => invoker.GetType().GetMethod(nameof(ConstructorInvoker.Invoke), [typeof(object), typeof(object), typeof(object)])!,
                            4 => invoker.GetType().GetMethod(nameof(ConstructorInvoker.Invoke), [typeof(object), typeof(object), typeof(object), typeof(object)])!,
                            _ => throw new InvalidOperationException(Res.InternalError("Unexpected number of parameters"))
                        },
                        methodParameters);

#else
                    // fallback to ConstructorInfo.Invoke(object[])
                    Expression[] methodParameters = [Expression.NewArrayInit(typeof(object), parameters.Select(p => p.Type == typeof(object) ? (Expression)p : Expression.Convert(p, typeof(object))))];
                    MethodCallExpression methodCall = Expression.Call(
                        Expression.Constant(ctor), // the instance is the ConstructorInfo itself
                        ctor.GetType().GetMethod(nameof(ConstructorInfo.Invoke), [typeof(object[])])!, // Invoke(object[])
                        methodParameters);

#endif
                    lambda = Expression.Lambda(delegateType, Expression.Convert(methodCall, ctor.DeclaringType!), parameters);
                    return lambda.Compile();
                }

                NewExpression construct = Expression.New(ctor, parameters);
                lambda = Expression.Lambda(delegateType, construct, parameters);
                return lambda.Compile();
            }
#endif

#if !NETSTANDARD2_0
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return dm.CreateDelegate(delegateType);
#endif
        }

        #endregion
    }
}
