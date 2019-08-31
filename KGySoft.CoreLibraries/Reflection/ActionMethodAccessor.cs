#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ActionMethodAccessor.cs
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
using System.Security;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Action method accessor invoker for any parameters.
    /// </summary>
    internal sealed class ActionMethodAccessor : MethodAccessor
    {
        #region Delegates

        /// <summary>
        /// Represents a non-generic action that can be used for any action methods (and constructors).
        /// </summary>
        private delegate void AnyAction(object target, object[] arguments);

        #endregion

        #region Constructors

        internal ActionMethodAccessor(MethodBase mi) // Now can be used for ctors but that is not cached in the base! See Accessors.GetCtorMethod
            : base(mi)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        public override object Invoke(object instance, params object[] parameters)
        {
            try
            {
                ((AnyAction)Invoker)(instance, parameters);
                return null;
            }
            catch (VerificationException e) when (IsSecurityConflict(e))
            {
                throw new NotSupportedException(Res.ReflectionSecuritySettingsConfict, e);
            }
        }

        #endregion

        #region Private Protected Methods

        private protected override Delegate CreateInvoker()
        {
            var methodBase = (MethodBase)MemberInfo;
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);
            Type declaringType = methodBase.DeclaringType;
            if (!methodBase.IsStatic && declaringType == null)
                throw new InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            var method  = methodBase as MethodInfo;
            if (method?.ReturnType.IsPointer == true)
                throw new NotSupportedException(Res.ReflectionPointerTypeNotSupported(method.ReturnType));

            // for classes and static methods that have no ref parameters: Lambda expression
            // ReSharper disable once PossibleNullReferenceException - declaring type was already checked above
            if (!hasRefParameters && (methodBase.IsStatic || !declaringType.IsValueType) && method != null)
            {
                ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
                ParameterExpression parametersParameter = Expression.Parameter(typeof(object[]), "parameters");
                var methodParameters = new Expression[ParameterTypes.Length];
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    //// This just avoids error when ref parameters are used but does not assign results back
                    //Type parameterType = ParameterTypes[i];
                    //if (parameterType.IsByRef)
                    //    parameterType = parameterType.GetElementType();
                    methodParameters[i] = Expression.Convert(Expression.ArrayIndex(parametersParameter, Expression.Constant(i)), ParameterTypes[i]);
                }

                // ReSharper disable once AssignNullToNotNullAttribute - declaring type was already checked above
                MethodCallExpression methodToCall = Expression.Call(
                        method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                        method, // method info
                        methodParameters); // parameters cast to target types

                LambdaExpression lambda = Expression.Lambda<AnyAction>(
                        methodToCall, // no return type
                        instanceParameter, // instance (object)
                        parametersParameter);
                return lambda.Compile();
            }

            // for struct instance methods, constructors or methods with ref/out parameters: Dynamic method
            var options = methodBase is ConstructorInfo ? DynamicMethodOptions.TreatCtorAsMethod : DynamicMethodOptions.None;
            if (hasRefParameters)
                options |= DynamicMethodOptions.HandleByRefParameters;
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(methodBase, options);
            return dm.CreateDelegate(typeof(AnyAction));
        }

        #endregion

        #endregion
    }
}
