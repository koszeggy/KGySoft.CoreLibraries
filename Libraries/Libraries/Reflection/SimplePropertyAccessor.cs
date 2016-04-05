using System;
using System.Reflection;
using System.Linq.Expressions;
using System.Reflection.Emit;

namespace KGySoft.Libraries.Reflection
{
    internal sealed class SimplePropertyAccessor: PropertyAccessor
    {
        /// <summary>
        /// Represents a non-generic setter that can be used for any simple properties.
        /// </summary>
        private delegate void PropertySetter(object instance, object value);

        /// <summary>
        /// Represents a non-generic getter that can be used for any simple properties.
        /// </summary>
        private delegate object PropertyGetter(object instance);

        /// <summary>
        /// Non-caching internal constructor. Called from cache.
        /// </summary>
        internal SimplePropertyAccessor(Type instanceType)
            : base(null, instanceType, null)
        {
        }

        protected override Delegate CreateGetter()
        {
            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo getterMethod = property.GetGetMethod(true);

            // for classes and static properties: Lambda expression
            if (!DeclaringType.IsValueType || getterMethod.IsStatic)
            {
                //---by property expression---
                ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");

                MemberExpression member = Expression.Property(
                    getterMethod.IsStatic ? null : Expression.Convert(instanceParameter, DeclaringType), // (TInstance)instance
                    (PropertyInfo)MemberInfo);

                LambdaExpression lambda = Expression.Lambda<PropertyGetter>(
                    Expression.Convert(member, typeof(object)), // object return type
                    instanceParameter); // instance (object)
                return lambda.Compile();

                ////---by getmethod---
                //PropertyInfo property = (PropertyInfo)MemberInfo;

                //ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");

                //MethodCallExpression getterCall = Expression.Call(
                //    Expression.Convert(instanceParameter, DeclaringType), // (TDeclaring)target
                //    getterMethod); // getter

                //LambdaExpression lambda = Expression.Lambda<PropertyGetter>(
                //    Expression.Convert(getterCall, typeof(object)), // object return type
                //    instanceParameter);   // instance (object)
                //return lambda.Compile();
            }
            // for struct instance properties: Dynamic method
            else
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.OmitParameters);
                return dm.CreateDelegate(typeof(PropertyGetter));
            }
        }

        protected override Delegate CreateSetter()
        {
            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo setterMethod = property.GetSetMethod(true);

            // for classes and static properties: Lambda expression
            if (!DeclaringType.IsValueType || setterMethod.IsStatic)
            {
                // .NET 4.0: Using Expression.Assign could be used, though it calls setter after all and will not work for struct instances
                //#  var param = Expression.Parameter(this.Type, "obj");
                //#             var value = Expression.Parameter(typeof(object), "val");
                //#  
                //#             var lambda = Expression.Lambda<Action<T, object>>(
                //#                 Expression.Assign(
                //#                     Expression.Property(param, propertyInfo),
                //#                     Expression.Convert(value, propertyInfo.PropertyType)),
                //#                 param, value);
                //#  
                //#             return lambda.Compile();

                // .NET 3.5: Calling the setter method
                ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "instance");
                ParameterExpression valueParameter = Expression.Parameter(typeof(object), "value");
                UnaryExpression castValue = Expression.Convert(valueParameter, property.PropertyType);

                MethodCallExpression setterCall = Expression.Call(
                    setterMethod.IsStatic ? null : Expression.Convert(instanceParameter, DeclaringType), // (TInstance)instance
                    setterMethod, // setter
                    castValue); // original parameter: (TProp)value

                LambdaExpression lambda = Expression.Lambda<PropertySetter>(
                    setterCall, // no return type
                    instanceParameter, // instance (object)
                    valueParameter);
                return lambda.Compile();
            }
            // for struct instance properties: Dynamic method
            else
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
                return dm.CreateDelegate(typeof(PropertySetter));
            }
        }

        public override void Set(object instance, object value, params object[] indexerParameters)
        {
            ((PropertySetter)Setter)(instance, value);
        }

        public override object Get(object instance, params object[] indexerParameters)
        {
            return ((PropertyGetter)Getter)(instance);
        }
    }
}
