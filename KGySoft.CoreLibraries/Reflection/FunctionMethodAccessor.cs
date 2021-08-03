#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FunctionMethodAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
#if !NETSTANDARD2_0
using System.Linq;
#endif
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;
using System.Security;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Function method accessor for any parameters.
    /// </summary>
    internal sealed class FunctionMethodAccessor : MethodAccessor
    {
        #region Delegates

        /// <summary>
        /// Represents a non-generic function that can be used for any function methods.
        /// </summary>
        private delegate object? AnyFunction(object? target, object?[]? arguments);

        #endregion

        #region Constructors

        internal FunctionMethodAccessor(MethodInfo mi)
            : base(mi)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override object? Invoke(object? instance, params object?[]? parameters)
        {
            try
            {
                return ((AnyFunction)Invoker)(instance, parameters);
            }
            catch (VerificationException e) when (IsSecurityConflict(e))
            {
                Throw.NotSupportedException(Res.ReflectionSecuritySettingsConflict, e);
                return default;
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

#if !NETSTANDARD2_0
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);

            // ReSharper disable once PossibleNullReferenceException - declaring type was already checked above
            if (hasRefParameters || (!method.IsStatic && declaringType!.IsValueType))
            {
                // for struct instance methods or methods with ref/out parameters: Dynamic method
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

            // ReSharper disable once AssignNullToNotNullAttribute - declaring type was already checked above
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

        #endregion

        #endregion
    }
}
