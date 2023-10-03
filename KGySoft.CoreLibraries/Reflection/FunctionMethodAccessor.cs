#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FunctionMethodAccessor.cs
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

#region Used Namespaces

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;

using KGySoft.CoreLibraries;

#endregion

#region Used Aliases

using AnyFunction = System.Func<object?, object?[]?, object?>;

#endregion

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

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public override object? Invoke(object? instance, params object?[]? parameters)
        {
            try
            {
                return ((AnyFunction)Invoker)(instance, parameters);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception
                PostValidate(instance, parameters, e);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        #endregion

        #region Private Protected Methods

        private protected override Delegate CreateInvoker()
        {
            MethodInfo method = (MethodInfo)MemberInfo;
            Type? declaringType = method.DeclaringType;
            if (!method.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (method.ReturnType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(method.ReturnType));

#if NETSTANDARD2_0
            if (method.ReturnType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(method.ReturnType));
#else
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);
            if (hasRefParameters || (!method.IsStatic && declaringType!.IsValueType) || method.ReturnType.IsByRef
#if NET40_OR_GREATER
                // Partially trusted app domain: to avoid VerificationException if SecurityPermissionFlag.SkipVerification is not granted
                || EnvironmentHelper.IsPartiallyTrustedDomain
#endif
                )
            {
                // for struct instance methods, methods with ref/out parameters or ref return types: Dynamic method
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, hasRefParameters ? DynamicMethodOptions.HandleByRefParameters : DynamicMethodOptions.None);
                return dm.CreateDelegate(typeof(AnyFunction));
            } 
#endif

            // for classes and static methods that have no ref parameters: Lambda expression
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "target");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var methodParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];
#if NETSTANDARD2_0
                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;

                // ReSharper disable once AssignNullToNotNullAttribute
#endif
                methodParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            MethodCallExpression methodToCall = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                method, // method info
                methodParameters); // arguments cast to target types

            LambdaExpression lambda = Expression.Lambda<AnyFunction>(
                Expression.Convert(methodToCall, Reflector.ObjectType), // return type converted to object
                instanceParameter, // instance (object)
                argumentsParameter);
            return lambda.Compile();
        }

        [SuppressMessage("ReSharper", "CoVariantArrayConversion", Justification = "Expression.Call does not write the parameters")]
        private protected override Delegate CreateGenericInvoker()
        {
            var method = (MethodInfo)Method;
            Type? declaringType = Method.DeclaringType;
            if (!Method.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (ParameterTypes.Length > 4 || ParameterTypes.Any(p => p.IsByRef))
                Throw.NotSupportedException(Res.ReflectionMethodGenericNotSupported);
            if (Method is MethodInfo { ReturnType.IsPointer: true })
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(method.ReturnType));
            if (ParameterTypes.FirstOrDefault(p => p.IsPointer) is Type pointerParam)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(pointerParam));

            ParameterExpression[] parameters;
            MethodCallExpression methodCall;
            LambdaExpression lambda;
            Type delegateType;

            // Static methods
            if (Method.IsStatic)
            {
                parameters = new ParameterExpression[ParameterTypes.Length];
                for (int i = 0; i < parameters.Length; i++)
                    parameters[i] = Expression.Parameter(ParameterTypes[i], $"param{i + 1}");
                methodCall = Expression.Call(null, method, parameters);
                delegateType = (ParameterTypes.Length switch
                {
                    0 => typeof(Func<>),
                    1 => typeof(Func<,>),
                    2 => typeof(Func<,,>),
                    3 => typeof(Func<,,,>),
                    4 => typeof(Func<,,,,>),
                    _ => Throw.InternalError<Type>("Unexpected number of parameters")
                }).GetGenericType(ParameterTypes.Append(method.ReturnType).ToArray());

                lambda = Expression.Lambda(delegateType, methodCall, parameters);
                return lambda.Compile();
            }

            parameters = new ParameterExpression[ParameterTypes.Length + 1];

            // Class instance methods
            if (!declaringType!.IsValueType)
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
                parameters[0] = Expression.Parameter(declaringType, "instance");
            }
            // Struct instance methods
            else
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
#if NET35
                // Expression.Call fails for .NET Framework 3.5 if the instance is a ByRef type so using DynamicMethod instead
                var dm = new DynamicMethod("<InvokeMethod>__" + method.Name, method.ReturnType,
                    new[] { declaringType.MakeByRefType() }.Concat(ParameterTypes).ToArray(), declaringType, true);
                ILGenerator il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0); // loading 0th argument: instance

                // loading parameters
                for (int i = 0; i < ParameterTypes.Length; i++)
                    il.Emit(OpCodes.Ldarg, i + 1);

                il.Emit(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method); // calling the method
                il.Emit(OpCodes.Ret); // returning method's return value
                return dm.CreateDelegate(delegateType.GetGenericType(new[] { declaringType }.Concat(ParameterTypes).Append(method.ReturnType).ToArray()));
#else
                parameters[0] = Expression.Parameter(declaringType.MakeByRefType(), "instance");
#endif
            }

            for (int i = 0; i < ParameterTypes.Length; i++)
                parameters[i + 1] = Expression.Parameter(ParameterTypes[i], $"param{i + 1}");

#if NET35
            methodCall = Expression.Call(parameters[0], method, parameters.Cast<Expression>().Skip(1));
#else
            methodCall = Expression.Call(parameters[0], method, parameters.Skip(1)); 
#endif
            delegateType = delegateType.GetGenericType(new[] { declaringType }.Concat(ParameterTypes).Append(method.ReturnType).ToArray());
            lambda = Expression.Lambda(delegateType, methodCall, parameters);
            return lambda.Compile();
        }

        #endregion

        #endregion
    }
}
