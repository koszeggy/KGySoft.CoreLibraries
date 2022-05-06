#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ActionMethodAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
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

using AnyAction = System.Action<object?, object?[]?>;

#endregion

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

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public override object? Invoke(object? instance, params object?[]? parameters)
        {
            try
            {
                ((AnyAction)Invoker)(instance, parameters);
                return null;
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
            var methodBase = (MethodBase)MemberInfo;
            Type? declaringType = methodBase.DeclaringType;
            if (!methodBase.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            var method = methodBase as MethodInfo;

#if NETSTANDARD2_0
            if (method == null)
                Throw.InternalError($"Constructors cannot be invoked by {nameof(ActionMethodAccessor)} in .NET Standard 2.0");
#else
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);

            // ReSharper disable once PossibleNullReferenceException - declaring type was already checked above
            if (hasRefParameters || (!methodBase.IsStatic && declaringType!.IsValueType) || method == null)
            {
                // For struct instance methods, constructors or methods with ref/out parameters: Dynamic method
                var options = methodBase is ConstructorInfo ? DynamicMethodOptions.TreatCtorAsMethod : DynamicMethodOptions.None;
                if (hasRefParameters)
                    options |= DynamicMethodOptions.HandleByRefParameters;
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(methodBase, options);
                return dm.CreateDelegate(typeof(AnyAction));
            }
#endif

            // For classes and static methods that have no ref parameters: Lambda expression
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var methodParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];
#if NETSTANDARD2_0
                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;
#endif
                methodParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            // ReSharper disable once AssignNullToNotNullAttribute - declaring type was already checked above
            MethodCallExpression methodToCall = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                method, // method info
                methodParameters); // parameters cast to target types

            LambdaExpression lambda = Expression.Lambda<AnyAction>(
                methodToCall, // no return type
                instanceParameter, // instance (object)
                argumentsParameter);
            return lambda.Compile();
        }

        private protected override Delegate CreateGenericInvoker()
        {
            if (Method is not MethodInfo method)
                return Throw.InternalError<Delegate>($"Constructor {Method} is not expected in {nameof(CreateGenericInvoker)}");

            Type? declaringType = method.DeclaringType;
            if (!method.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (ParameterTypes.Length > 4 || ParameterTypes.Any(p => p.IsByRef))
                Throw.NotSupportedException(Res.ReflectionMethodGenericNotSupported);
            if (method.ReturnType.IsPointer == true)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(method.ReturnType));
            if (ParameterTypes.FirstOrDefault(p => p.IsPointer) is Type pointerParam)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(pointerParam));

            ParameterExpression[] parameters;
            MethodCallExpression methodCall;
            LambdaExpression lambda;
            Type delegateType;

            // Static methods
            if (method.IsStatic)
            {
                parameters = new ParameterExpression[ParameterTypes.Length];
                for (int i = 0; i < parameters.Length; i++)
                    parameters[i] = Expression.Parameter(ParameterTypes[i], $"param{i + 1}");
                methodCall = Expression.Call(null, method, parameters);
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
                    delegateType = delegateType.GetGenericType(ParameterTypes);
                lambda = Expression.Lambda(delegateType, methodCall, parameters);
                return lambda.Compile();
            }

            parameters = new ParameterExpression[ParameterTypes.Length + 1];
            for (int i = 0; i < ParameterTypes.Length; i++)
                parameters[i + 1] = Expression.Parameter(ParameterTypes[i], $"param{i + 1}");

            // Class instance methods
            if (!declaringType!.IsValueType)
            {
                parameters[0] = Expression.Parameter(declaringType, "instance");
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
            // Struct instance methods
            else
            {
                parameters[0] = Expression.Parameter(declaringType.MakeByRefType(), "instance");
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

#if NET35
            methodCall = Expression.Call(parameters[0], method, parameters.Cast<Expression>().Skip(1));
#else
            methodCall = Expression.Call(parameters[0], method, parameters.Skip(1)); 
#endif
            delegateType = delegateType.GetGenericType(new[] { declaringType }.Concat(ParameterTypes).ToArray());
            lambda = Expression.Lambda(delegateType, methodCall, parameters);
            return lambda.Compile();
        }

        #endregion

        #endregion
    }
}
