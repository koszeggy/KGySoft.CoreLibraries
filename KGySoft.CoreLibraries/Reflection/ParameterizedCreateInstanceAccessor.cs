#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParameterizedCreateInstanceAccessor.cs
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
    /// Object factory for creating new instance of an object via a specified constructor.
    /// </summary>
    internal sealed class ParameterizedCreateInstanceAccessor : CreateInstanceAccessor
    {
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
                ThrowIfNotSupportedParameters();
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
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.None);
            return (Func<object?[]?, object>)dm.CreateDelegate(typeof(Func<object?[]?, object>));
#endif
        }

        private protected override Delegate CreateNonGenericInitializer()
        {
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
                ThrowIfNotSupportedParameters();
                return ParameterTypes.Length switch
                {
                    0 => new Func<object?>(() => ctor.Invoke(null)),
                    1 => new Func<object?, object?>(p => ctor.Invoke([p])),
                    2 => new Func<object?, object?, object?>((p1, p2) => ctor.Invoke([p1, p2])),
                    3 => new Func<object?, object?, object?, object?>((p1, p2, p3) => ctor.Invoke([p1, p2, p3])),
                    4 => new Func<object?, object?, object?, object?, object?>((p1, p2, p3, p4) => ctor.Invoke([p1, p2, p3, p4])),
                    _ => Throw.InternalError<Delegate>("Unexpected number of parameters")
                };
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

#if NETSTANDARD2_0
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

            // The constructor has pointer parameters: fallback to ConstructorInfo.Invoke(object[]), which supports pointer parameters as IntPtr...
            if (ParameterTypes.Any(p => p.IsPointer))
            {
                ThrowIfNotSupportedParameters(); // ...except ref pointers

                Expression[] methodParameters = [Expression.NewArrayInit(typeof(object), parameters.Select(p => Expression.Convert(p, typeof(object))))];
                MethodCallExpression methodCall = Expression.Call(
                    Expression.Constant(ctor), // the instance is the ConstructorInfo itself
                    ctor.GetType().GetMethod(nameof(ConstructorInfo.Invoke), [typeof(object[])])!, // Invoke(object[])
                    methodParameters);

                lambda = Expression.Lambda(delegateType, Expression.Convert(methodCall, ctor.DeclaringType!), parameters);
                return lambda.Compile();
            }

            NewExpression construct = Expression.New(ctor, parameters);
            lambda = Expression.Lambda(delegateType, construct, parameters);
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return dm.CreateDelegate(delegateType);
#endif
        }

        #endregion
    }
}
