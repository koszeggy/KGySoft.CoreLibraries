#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: ParameterizedCreateInstanceAccessor.cs
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
    /// Object factory for creating new instance of an object via a specified constructor.
    /// </summary>
    internal sealed class ParameterizedCreateInstanceAccessor : CreateInstanceAccessor
    {
        #region Delegates

        /// <summary>
        /// Represents a constructor.
        /// </summary>
        private delegate object Ctor(object[] arguments);

        #endregion

        #region Constructors

        internal ParameterizedCreateInstanceAccessor(ConstructorInfo ctor)
            : base(ctor)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        public override object CreateInstance(params object[] parameters)
            => ((Ctor)Initializer)(parameters);

        #endregion

        #region Private Protected Methods

        /// <summary>
        /// Creates object initialization delegate.
        /// </summary>
        private protected override Delegate CreateInitializer()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);

            // for constructors that have no ref parameters: Lambda expression
            if (!hasRefParameters)
            {
                ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
                var ctorParameters = new Expression[ParameterTypes.Length];
                for (int i = 0; i < ParameterTypes.Length; i++)
                    ctorParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), ParameterTypes[i]);

                NewExpression construct = Expression.New(
                        ctor, // constructor info
                        ctorParameters); // arguments cast to target types

                LambdaExpression lambda = Expression.Lambda<Ctor>(
                        Expression.Convert(construct, Reflector.ObjectType), // return type converted to object
                        argumentsParameter);
                return lambda.Compile();
            }

            // for constructors with ref/out parameters: Dynamic method
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.HandleByRefParameters);
            return dm.CreateDelegate(typeof(Ctor));
        }

        #endregion

        #endregion
    }
}
