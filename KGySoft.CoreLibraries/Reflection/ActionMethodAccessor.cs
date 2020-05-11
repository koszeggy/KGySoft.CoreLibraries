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
using System.Runtime.CompilerServices;
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

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override object Invoke(object instance, params object[] parameters)
        {
            try
            {
                ((AnyAction)Invoker)(instance, parameters);
                return null;
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
            var methodBase = (MethodBase)MemberInfo;
            Type declaringType = methodBase.DeclaringType;
            if (!methodBase.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            var method  = methodBase as MethodInfo;
            if (method?.ReturnType.IsPointer == true)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(method.ReturnType));

#if NETSTANDARD2_0
            if (method == null)
                Throw.InternalError($"Constructors cannot be invoked by {nameof(ActionMethodAccessor)} in .NET Standard 2.0");
#else
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);

            // ReSharper disable once PossibleNullReferenceException - declaring type was already checked above
            if (hasRefParameters || (!methodBase.IsStatic && declaringType.IsValueType) || method == null)
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
                    parameterType = parameterType.GetElementType();

                // ReSharper disable once AssignNullToNotNullAttribute
#endif
                methodParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            // ReSharper disable once AssignNullToNotNullAttribute - declaring type was already checked above
            MethodCallExpression methodToCall = Expression.Call(
                method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                method, // method info
                methodParameters); // parameters cast to target types

            LambdaExpression lambda = Expression.Lambda<AnyAction>(
                methodToCall, // no return type
                instanceParameter, // instance (object)
                argumentsParameter);
            return lambda.Compile();
        }

#endregion

#endregion
    }
}
