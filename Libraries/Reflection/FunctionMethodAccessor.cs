using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace KGySoft.Reflection
{
    /// <summary>
    /// Function method accessor for any parameters. Internal, cannot be instantiated from outside.
    /// </summary>
    internal sealed class FunctionMethodAccessor : MethodAccessor
    {
        /// <summary>
        /// Represents a non-generic function that can be used for any function methods.
        /// </summary>
        private delegate object AnyFunction(object target, object[] arguments);

        /// <summary>
        /// Non-caching internal constructor. Called from cache.
        /// </summary>
        internal FunctionMethodAccessor(MethodInfo mi)
            : base(mi)
        {
        }

        protected override Delegate CreateInvoker()
        {
            MethodInfo method = (MethodInfo)MemberInfo;
            Type declaringType = method.DeclaringType;
            if (!method.IsStatic && declaringType == null)
                throw new InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            bool hasRefParameters = ParameterTypes.Any(p => p.IsByRef);

            // for classes and static methods that have no ref parameters: Lambda expression
            if (!hasRefParameters && (method.IsStatic || !declaringType.IsValueType))
            {
                ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");
                ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");
                UnaryExpression[] methodParameters = new UnaryExpression[ParameterTypes.Length];
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    methodParameters[i] = Expression.Convert(Expression.ArrayIndex(argumentsParameter, Expression.Constant(i)), ParameterTypes[i]);
                }

                MethodCallExpression methodToCall = Expression.Call(
                    method.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                    method, // method info
                    methodParameters); // arguments casted to target types

                LambdaExpression lambda = Expression.Lambda<AnyFunction>(
                    Expression.Convert(methodToCall, typeof(object)), // return type converted to object
                    instanceParameter, // instance (object)
                    argumentsParameter);
                return lambda.Compile();
            }
            // for struct instance methods or methods with ref/out parameters: Dynamic method
            else
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(method, hasRefParameters ? DynamicMethodOptions.HandleByRefParameters : DynamicMethodOptions.None);
                return dm.CreateDelegate(typeof(AnyFunction));
            }
        }

        public override object Invoke(object instance, params object[] parameters)
        {
            return ((AnyFunction)Invoker)(instance, parameters);
        }
    }
}
