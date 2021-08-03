#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParameterizedCreateInstanceAccessor.cs
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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Object factory for creating new instance of an object via a specified constructor.
    /// </summary>
    internal sealed class ParameterizedCreateInstanceAccessor : CreateInstanceAccessor
    {
        #region Delegates

        /// <summary>
        /// Represents a constructor.
        /// </summary>
        private delegate object Ctor(object?[]? arguments);

        #endregion

        #region Constructors

        internal ParameterizedCreateInstanceAccessor(ConstructorInfo ctor)
            : base(ctor)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public override object CreateInstance(params object?[]? parameters)
            => ((Ctor)Initializer)(parameters);

        #endregion

        #region Private Protected Methods

        /// <summary>
        /// Creates object initialization delegate.
        /// </summary>
        private protected override Delegate CreateInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;

#if !NETSTANDARD2_0
            // for constructors with ref/out parameters: Dynamic method
            if (ParameterTypes.Any(p => p.IsByRef))
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.HandleByRefParameters);
                return dm.CreateDelegate(typeof(Ctor));
            }
#endif

            // for constructors that have no ref parameters: Lambda expression
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
            var ctorParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
            {
                Type parameterType = ParameterTypes[i];
#if NETSTANDARD2_0
                // This just avoids error when ref parameters are used but does not assign results back
                if (parameterType.IsByRef)
                    parameterType = parameterType.GetElementType()!;
#endif
                ctorParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), parameterType);
            }

            NewExpression construct = Expression.New(
                ctor, // constructor info
                ctorParameters); // arguments cast to target types

            LambdaExpression lambda = Expression.Lambda<Ctor>(
                Expression.Convert(construct, Reflector.ObjectType), // return type converted to object
                argumentsParameter);
            return lambda.Compile();
        }

        #endregion

        #endregion
    }
}
