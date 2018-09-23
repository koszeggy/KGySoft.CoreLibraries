using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Object factory for creating new instance of an object via a specified constructor.
    /// Internal, cannot be instantiated from outside.
    /// </summary>
    internal sealed class ObjectFactoryParameterized: ObjectFactory
    {
        /// <summary>
        /// Represents a constructor.
        /// </summary>
        private delegate object Ctor(object[] arguments);

        /// <summary>
        /// Non-caching internal constructor. Called from cache.
        /// </summary>
        internal ObjectFactoryParameterized(Type instanceType, Type[] parameterTypes)
            : base(null, instanceType, parameterTypes)
        {
        }

        /// <summary>
        /// Creates object initialization delegate. Stored MemberInfo is a Type so it works
        /// also in case of value types where actually there is no parameterless constructor.
        /// </summary>
        protected override Delegate CreateFactory()
        {
            ConstructorInfo ctor = (ConstructorInfo)MemberInfo;
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);

            // for constructors that have no ref parameters: Lambda expression
            if (!hasRefParameters)
            {
                ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
                UnaryExpression[] ctorParameters = new UnaryExpression[ParameterTypes.Length];
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    ctorParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), ParameterTypes[i]);
                }

                NewExpression construct = Expression.New(
                    ctor, // constructor info
                    ctorParameters); // arguments casted to target types

                LambdaExpression lambda = Expression.Lambda<Ctor>(
                    Expression.Convert(construct, typeof(object)), // return type converted to object
                    argumentsParameter);
                return lambda.Compile();
            }
            // for constructors with ref/out parameters: Dynamic method
            else
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(ctor, DynamicMethodOptions.HandleByRefParameters);
                return dm.CreateDelegate(typeof(Ctor));
            }
        }

        public override object Create(params object[] parameters)
        {
            return ((Ctor)Factory)(parameters);
        }

    }
}
