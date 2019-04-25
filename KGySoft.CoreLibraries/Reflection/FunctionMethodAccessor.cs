#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FunctionMethodAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2019 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution. If not, then this file is considered as
//  an illegal copy.
//
//  Unauthorized copying of this file, via any medium is strictly prohibited.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

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
        private delegate object AnyFunction(object target, object[] arguments);

        #endregion

        #region Constructors

        internal FunctionMethodAccessor(MethodInfo mi)
            : base(mi)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        public override object Invoke(object instance, params object[] parameters)
            => ((AnyFunction)Invoker)(instance, parameters);

        #endregion

        #region Internal Methods

        internal /*private protected*/ override Delegate CreateInvoker()
        {
            MethodInfo method = (MethodInfo)MemberInfo;
            Type declaringType = method.DeclaringType;
            if (!method.IsStatic && declaringType == null)
                throw new InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);

            // for classes and static methods that have no ref parameters: Lambda expression
            // ReSharper disable once PossibleNullReferenceException - declaring type was already checked above
            if (!hasRefParameters && (method.IsStatic || !declaringType.IsValueType))
            {
                ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "target");
                ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
                var methodParameters = new Expression[ParameterTypes.Length];
                for (int i = 0; i < ParameterTypes.Length; i++)
                    methodParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), ParameterTypes[i]);

                // ReSharper disable once AssignNullToNotNullAttribute - declaring type was already checked above
                MethodCallExpression methodToCall = Expression.Call(
                        method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                        method, // method info
                        methodParameters); // arguments cast to target types

                LambdaExpression lambda = Expression.Lambda<AnyFunction>(
                        Expression.Convert(methodToCall, Reflector.ObjectType), // return type converted to object
                        instanceParameter, // instance (object)
                        argumentsParameter);
                return lambda.Compile();
            }

            // for struct instance methods or methods with ref/out parameters: Dynamic method
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, hasRefParameters ? DynamicMethodOptions.HandleByRefParameters : DynamicMethodOptions.None);
            return dm.CreateDelegate(typeof(AnyFunction));
        }

        #endregion

        #endregion
    }
}
