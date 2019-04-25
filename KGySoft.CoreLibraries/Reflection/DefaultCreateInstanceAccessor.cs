#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: DefaultCreateInstanceAccessor.cs
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
using System.Linq.Expressions;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Object factory for creating new instance of an object with default constructor
    /// or without constructor (for value types).
    /// </summary>
    internal sealed class DefaultCreateInstanceAccessor : CreateInstanceAccessor
    {
        #region Delegates

        /// <summary>
        /// Represents a default constructor.
        /// </summary>
        private delegate object DefaultCtor();

        #endregion

        #region Constructors

        internal DefaultCreateInstanceAccessor(Type instanceType)
            : base(instanceType)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        public override object CreateInstance(params object[] parameters) => ((DefaultCtor)Initializer)();

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates object initialization delegate. Stored MemberInfo is a Type so it works
        /// also in case of value types where actually there is no parameterless constructor.
        /// </summary>
        internal /*private protected*/ override Delegate CreateInitializer()
        {
            NewExpression construct = Expression.New((Type)MemberInfo);
            LambdaExpression lambda = Expression.Lambda<DefaultCtor>(
                    Expression.Convert(construct, Reflector.ObjectType)); // return type converted to object
            return lambda.Compile();
        }

        #endregion

        #endregion
    }
}
