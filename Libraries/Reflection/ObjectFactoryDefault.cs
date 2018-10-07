using System;
using System.Linq.Expressions;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Object factory for creating new instance of an object with default constructor
    /// or without constructor (for value types). Internal, cannot be instantiated from outside.
    /// </summary>
    internal sealed class ObjectFactoryDefault: ObjectFactory
    {
        /// <summary>
        /// Represents a default constructor.
        /// </summary>
        private delegate object DefaultCtor();

        internal ObjectFactoryDefault(Type instanceType)
            : base(instanceType)
        {
        }

        /// <summary>
        /// Creates object initialization delegate. Stored MemberInfo is a Type so it works
        /// also in case of value types where actually there is no parameterless constructor.
        /// </summary>
        protected override Delegate CreateFactory()
        {
            NewExpression construct = Expression.New((Type)MemberInfo);
            LambdaExpression lambda = Expression.Lambda<DefaultCtor>(
                Expression.Convert(construct, typeof(object))); // return type converted to object
            return lambda.Compile();
        }

        public override object Create(params object[] parameters)
        {
            return ((DefaultCtor)Factory)();
        }
    }
}
