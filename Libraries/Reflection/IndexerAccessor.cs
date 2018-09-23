using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace KGySoft.Reflection
{
    internal sealed class IndexerAccessor: PropertyAccessor
    {
        /// <summary>
        /// Represents a non-generic setter that can be used for any indexers.
        /// </summary>
        private delegate void IndexerSetter(object instance, object value, object[] indexArguments);

        /// <summary>
        /// Represents a non-generic getter that can be used for any indexers.
        /// </summary>
        private delegate object IndexerGetter(object instance, object[] indexArguments);

        /// <summary>
        /// Non-caching internal constructor. Called from cache.
        /// </summary>
        internal IndexerAccessor(Type instanceType, Type[] parameterTypes)
            : base(null, instanceType, parameterTypes)
        {
        }

        protected override Delegate CreateGetter()
        {
            PropertyInfo property = (PropertyInfo)MemberInfo;

            MethodInfo getterMethod = property.GetGetMethod(true);

            // for classes: Lambda expression
            if (!DeclaringType.IsValueType)
            {

                ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
                ParameterExpression indexArgumentsParameter = Expression.Parameter(typeof(object[]), "indexArguments");
                UnaryExpression[] getterParameters = new UnaryExpression[ParameterTypes.Length];
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    getterParameters[i] = Expression.Convert(Expression.ArrayIndex(indexArgumentsParameter, Expression.Constant(i)), ParameterTypes[i]);
                }

                MethodCallExpression getterCall = Expression.Call(
                    Expression.Convert(instanceParameter, this.DeclaringType), // (TInstance)instance
                    getterMethod, // getter
                    getterParameters); // arguments casted to target types

                LambdaExpression lambda = Expression.Lambda<IndexerGetter>(
                    Expression.Convert(getterCall, typeof(object)), // object return type
                    instanceParameter, // instance (object)
                    indexArgumentsParameter); // index parameters (object[])
                return lambda.Compile();
            }
            // for structs: Dynamic method
            else
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.None);
                return dm.CreateDelegate(typeof(IndexerGetter));
            }
        }

        protected override Delegate CreateSetter()
        {
            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo setterMethod = property.GetSetMethod(true);

            // for classes: Lambda expression
            if (!DeclaringType.IsValueType)
            {
                ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
                ParameterExpression valueParameter = Expression.Parameter(typeof(object), "value");
                ParameterExpression indexArgumentsParameter = Expression.Parameter(typeof(object[]), "indexArguments");

                // indexer parameters
                UnaryExpression[] setterParameters = new UnaryExpression[ParameterTypes.Length + 1]; // +1: value to set after indices
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    setterParameters[i] = Expression.Convert(Expression.ArrayIndex(indexArgumentsParameter, Expression.Constant(i)), ParameterTypes[i]);
                }
                // value parameter is the last one
                setterParameters[ParameterTypes.Length] = Expression.Convert(valueParameter, property.PropertyType);

                MethodCallExpression setterCall = Expression.Call(
                    Expression.Convert(instanceParameter, DeclaringType), // (TInstance)instance
                    setterMethod, // setter
                    setterParameters); // arguments casted to target types + value as last argument casted to property type

                LambdaExpression lambda = Expression.Lambda<IndexerSetter>(
                    setterCall, // no return type
                    instanceParameter, // instance (object)
                    valueParameter, // value (object)
                    indexArgumentsParameter); // index parameters (object[])
                return lambda.Compile();
            }
            // for structs: Dynamic method
            else
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
                return dm.CreateDelegate(typeof(IndexerSetter));
            }
        }

        public override void Set(object instance, object value, params object[] indexerParameters)
        {
            ((IndexerSetter)Setter)(instance, value, indexerParameters);
        }

        public override object Get(object instance, params object[] indexerParameters)
        {
            return ((IndexerGetter)Getter)(instance, indexerParameters);
        }
    }
}
