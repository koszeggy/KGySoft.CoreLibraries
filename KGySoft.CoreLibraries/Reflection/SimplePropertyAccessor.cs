#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SimplePropertyAccessor.cs
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
using System.Reflection;
using System.Reflection.Emit;

#endregion

namespace KGySoft.Reflection
{
    internal sealed class SimplePropertyAccessor : PropertyAccessor
    {
        #region Delegates

        /// <summary>
        /// Represents a non-generic setter that can be used for any simple properties.
        /// </summary>
        private delegate void PropertySetter(object instance, object value);

        /// <summary>
        /// Represents a non-generic getter that can be used for any simple properties.
        /// </summary>
        private delegate object PropertyGetter(object instance);

        #endregion

        #region Constructors

        internal SimplePropertyAccessor(PropertyInfo pi)
            : base(pi)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        public override void Set(object instance, object value, params object[] indexerParameters)
            => ((PropertySetter)Setter)(instance, value);

        public override object Get(object instance, params object[] indexerParameters)
            => ((PropertyGetter)Getter)(instance);

        #endregion

        #region Protected Methods

        internal /*private protected*/ override Delegate CreateGetter()
        {
            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo getterMethod = property.GetGetMethod(true);
            Type declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                throw new InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                throw new NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));

            // for classes and static properties: Lambda expression
            if (!declaringType.IsValueType || getterMethod.IsStatic)
            {
                //---by property expression---
                ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");

                MemberExpression member = Expression.Property(
                        getterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                        (PropertyInfo)MemberInfo);

                LambdaExpression lambda = Expression.Lambda<PropertyGetter>(
                        Expression.Convert(member, Reflector.ObjectType), // object return type
                        instanceParameter); // instance (object)
                return lambda.Compile();

                ////---by calling the getter method---
                //ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");

                //MethodCallExpression getterCall = Expression.Call(
                //    Expression.Convert(instanceParameter, declaringType), // (TDeclaring)target
                //    getterMethod); // getter

                //LambdaExpression lambda = Expression.Lambda<PropertyGetter>(
                //    Expression.Convert(getterCall, Reflector.ObjectType), // object return type
                //    instanceParameter);   // instance (object)
                //return lambda.Compile();
            }

            // for struct instance properties: Dynamic method
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.OmitParameters);
            return dm.CreateDelegate(typeof(PropertyGetter));
        }

        internal /*private protected*/ override Delegate CreateSetter()
        {
            PropertyInfo property = (PropertyInfo)MemberInfo;
            MethodInfo setterMethod = property.GetSetMethod(true);
            Type declaringType = setterMethod.DeclaringType;
            if (declaringType == null)
                throw new InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (property.PropertyType.IsPointer)
                throw new NotSupportedException(Res.ReflectionPointerTypeNotSupported(property.PropertyType));

            // for classes and static properties: Lambda expression
            if (!declaringType.IsValueType || setterMethod.IsStatic)
            {
                // Calling the setter method (works even in .NET 3.5, while Assign is available from .NET 4 only)
                ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
                ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
                UnaryExpression castValue = Expression.Convert(valueParameter, property.PropertyType);

                MethodCallExpression setterCall = Expression.Call(
                        setterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                        setterMethod, // setter
                        castValue); // original parameter: (TProp)value

                LambdaExpression lambda = Expression.Lambda<PropertySetter>(
                        setterCall, // no return type
                        instanceParameter, // instance (object)
                        valueParameter);
                return lambda.Compile();
            }

            // for struct instance properties: Dynamic method
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
            return dm.CreateDelegate(typeof(PropertySetter));
        }

        #endregion

        #endregion
    }
}
