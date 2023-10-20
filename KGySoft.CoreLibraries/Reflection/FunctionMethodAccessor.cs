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
            if (method.ReturnType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(method.ReturnType));

#if NETSTANDARD2_0
            if (method.ReturnType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(method.ReturnType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "target");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var methodParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];
           
                // This just avoids error when ref parameters are used but does not assign results back
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
            if (method.ReturnType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(method.ReturnType));
            if (ParameterTypes.FirstOrDefault(p => p.IsPointer) is Type pointerParam)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(pointerParam));

#if NETSTANDARD2_0
    throw new NotImplementedException("CreateNonGenericSpecializedInvoker - expressions");
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, DynamicMethodOptions.ExactParameters);
            Type delegateType = ParameterTypes.Length switch
            {
                0 => typeof(Func<object?, object?>),
                1 => typeof(Func<object?, object?, object?>),
                2 => typeof(Func<object?, object?, object?, object?>),
                3 => typeof(Func<object?, object?, object?, object?, object?>),
                4 => typeof(Func<object?, object?, object?, object?, object?, object?>),
                _ => Throw.InternalError<Type>("Unexpected number of parameters")
            };

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
            if (Method is MethodInfo { ReturnType.IsPointer: true })
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(method.ReturnType));
            if (ParameterTypes.FirstOrDefault(p => p.IsPointer) is Type pointerParam)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(pointerParam));

            bool isByRef = method.ReturnType.IsByRef;
            Type returnType = isByRef ? method.ReturnType.GetElementType()! : method.ReturnType;

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
                }).GetGenericType(StripByRefTypes(ParameterTypes)
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
                    .Concat(StripByRefTypes(ParameterTypes))
                    .Append(returnType)
                    .ToArray());
            }

#if NETSTANDARD2_0
            if (isByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(method.ReturnType));

            ParameterExpression[] parameters;
            MethodCallExpression methodCall;
            LambdaExpression lambda;

            // Static methods
            if (isStatic)
            {
                parameters = new ParameterExpression[ParameterTypes.Length];
                for (int i = 0; i < parameters.Length; i++)
                    parameters[i] = Expression.Parameter(ParameterTypes[i], $"param{i + 1}");
                methodCall = Expression.Call(null, method, parameters);

                lambda = Expression.Lambda(delegateType, methodCall, parameters);
                return lambda.Compile();
            }

            // Instance methods
            parameters = new ParameterExpression[ParameterTypes.Length + 1];

            if (!isValueType)
                parameters[0] = Expression.Parameter(declaringType!, "instance");
            else
                parameters[0] = Expression.Parameter(declaringType!.MakeByRefType(), "instance");

            for (int i = 0; i < ParameterTypes.Length; i++)
                parameters[i + 1] = Expression.Parameter(ParameterTypes[i], $"param{i + 1}");

            methodCall = Expression.Call(parameters[0], method, parameters.Skip(1));
            delegateType = delegateType.GetGenericType(new[] { declaringType! }.Concat(ParameterTypes).Append(returnType).ToArray());
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
