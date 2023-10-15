#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IndexerAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2023 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Reflection
{
    internal sealed class IndexerAccessor : PropertyAccessor
    {
        #region Constructors

        internal IndexerAccessor(PropertyInfo pi)
            : base(pi)
        {
        }

        #endregion

        #region Methods

        #region Private Protected Methods

        private protected override Action<object?, object?, object?[]?> CreateGeneralSetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            if (!CanWrite)
            {
                if (Property.PropertyType.IsByRef)
                {
#if NETSTANDARD2_0
                    Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
                    DynamicMethod dm = CreateSetRefAsDynamicMethod(null);
                    return (Action<object?, object?, object?[]?>)dm.CreateDelegate(typeof(Action<object?, object?, object?[]?>));
#endif
                }

                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            }

            MethodInfo setterMethod = Property.GetSetMethod(true)!;

#if NETSTANDARD2_0
            if (declaringType.IsValueType)
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructPropertyNetStandard20(Property.Name, declaringType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            ParameterExpression indexParametersParameter = Expression.Parameter(typeof(object[]), "indexParameters");

            // indexer parameters
            var setterParameters = new Expression[ParameterTypes.Length + 1]; // +1: value to set after indices
            for (int i = 0; i < ParameterTypes.Length; i++)
                setterParameters[i] = Expression.Convert(Expression.ArrayIndex(indexParametersParameter, Expression.Constant(i)), ParameterTypes[i]);

            // value parameter is the last one
            setterParameters[ParameterTypes.Length] = Expression.Convert(valueParameter, Property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                setterMethod, // setter
                setterParameters); // arguments cast to target types + value as last argument cast to property type

            var lambda = Expression.Lambda<Action<object?, object?, object?[]?>>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter, // value (object)
                indexParametersParameter); // indexParameters (object[])
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
            return (Action<object?, object?, object?[]?>)result.CreateDelegate(typeof(Action<object?, object?, object?[]?>));
#endif
        }

        private protected override Func<object?, object?[]?, object?> CreateGeneralGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

#if NETSTANDARD2_0
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression indexParametersParameter = Expression.Parameter(typeof(object[]), "indexParameters");
            var getterParameters = new Expression[ParameterTypes.Length];
            for (int i = 0; i < ParameterTypes.Length; i++)
                getterParameters[i] = Expression.Convert(Expression.ArrayIndex(indexParametersParameter, Expression.Constant(i)), ParameterTypes[i]);

            MethodCallExpression getterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                getterMethod, // getter
                getterParameters); // arguments cast to target types

            var lambda = Expression.Lambda<Func<object?, object?[]?, object?>>(
                Expression.Convert(getterCall, Reflector.ObjectType), // object return type
                instanceParameter, // instance (object)
                indexParametersParameter); // indexParameters (object[])
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.None);
            return (Func<object?, object?[]?, object?>)dm.CreateDelegate(typeof(Func<object?, object?[]?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericSetter()
        {
            // The 1 parameter overload was called for a more-params indexer
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(); // Will be handled in PostValidate

            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

            if (!CanWrite)
            {
                if (Property.PropertyType.IsByRef)
                {
#if NETSTANDARD2_0
                    Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
                    DynamicMethod dm = CreateSetRefAsDynamicMethod(false);
                    return (Action<object?, object?, object?>)dm.CreateDelegate(typeof(Action<object?, object?, object?>));
#endif
                }

                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            }

            MethodInfo setterMethod = Property.GetSetMethod(true)!;

#if NETSTANDARD2_0
            if (declaringType.IsValueType)
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructPropertyNetStandard20(Property.Name, declaringType));

            // for classes: Lambda expression
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            ParameterExpression indexParameter = Expression.Parameter(Reflector.ObjectType, "index");

            // indexer parameters
            var setterParameters = new Expression[2]; // index, value
            setterParameters[0] = Expression.Convert(indexParameter, ParameterTypes[0]);
            setterParameters[1] = Expression.Convert(valueParameter, Property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                setterMethod, // setter
                setterParameters); // arguments cast to target types + value as last argument cast to property type

            var lambda = Expression.Lambda<Action<object?, object?, object?>>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter, // value (object)
                indexParameter); // index (object)
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters);
            return (Action<object?, object?, object?>)result.CreateDelegate(typeof(Action<object?, object?, object?>));
#endif
        }

        private protected override Delegate CreateNonGenericGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));

            // The 1 parameter overload was called for a more-params indexer
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(); // Will be handled in PostValidate

            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

#if NETSTANDARD2_0
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression indexParameter = Expression.Parameter(Reflector.ObjectType, "index");

            MethodCallExpression getterCall = Expression.Call(
                Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                getterMethod, // getter
                Expression.Convert(indexParameter, ParameterTypes[0])); // index cast to the parameter type

            var lambda = Expression.Lambda<Func<object?, object?, object?>>(
                Expression.Convert(getterCall, Reflector.ObjectType), // object return type
                instanceParameter, // instance (object)
                indexParameter); // index (object)
            return lambda.Compile();
#else
            DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.ExactParameters);
            return (Func<object?, object?, object?>)dm.CreateDelegate(typeof(Func<object?, object?, object?>));
#endif

        }

        private protected override Delegate CreateGenericSetter()
        {
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(Res.ReflectionIndexerGenericNotSupported);

            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            bool isValueType = declaringType.IsValueType;
            Type delegateType = (isValueType ? typeof(ValueTypeAction<,,>) : typeof(ReferenceTypeAction<,,>))
                .GetGenericType(declaringType, Property.PropertyType.IsByRef ? Property.PropertyType.GetElementType()! : Property.PropertyType, ParameterTypes[0]);

            if (!CanWrite)
            {
                if (Property.PropertyType.IsByRef)
                {
#if NETSTANDARD2_0
                    Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));
#else
                    DynamicMethod dm = CreateSetRefAsDynamicMethod(true);
                    return dm.CreateDelegate(delegateType);
#endif
                }

                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            }

            MethodInfo setterMethod = Property.GetSetMethod(true)!;

#if NETSTANDARD2_0
            ParameterExpression instanceParameter = Expression.Parameter(isValueType ? declaringType.MakeByRefType() : declaringType, "instance");
            ParameterExpression indexParameter = Expression.Parameter(ParameterTypes[0], "index");
            ParameterExpression valueParameter = Expression.Parameter(Property.PropertyType, "value");
            MethodCallExpression setterCall = Expression.Call(instanceParameter, setterMethod, indexParameter, valueParameter);
            LambdaExpression lambda = Expression.Lambda(delegateType, setterCall, instanceParameter, valueParameter, indexParameter);
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif
        }

        private protected override Delegate CreateGenericGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(Res.ReflectionIndexerGenericNotSupported);

            bool isValueType = declaringType.IsValueType;
            bool isRefReturn = Property.PropertyType.IsByRef;
            Type returnType = isRefReturn ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            Type delegateType = (isValueType ? typeof(ValueTypeFunction<,,>) : typeof(ReferenceTypeFunction<,,>))
                .GetGenericType(declaringType, ParameterTypes[0], returnType);

#if NETSTANDARD2_0
            if (isRefReturn)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            // Note: Expression.Call works everywhere but .NET Framework 3.5 if the instance is a ByRef type
            ParameterExpression instanceParameter = Expression.Parameter(isValueType ? declaringType.MakeByRefType() : declaringType, "instance");
            ParameterExpression indexParameter = Expression.Parameter(ParameterTypes[0], "index");
            MethodCallExpression getterCall = Expression.Call(instanceParameter, getterMethod, indexParameter);
            LambdaExpression lambda = Expression.Lambda(delegateType, getterCall, instanceParameter, indexParameter);
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif
        }

        #endregion

        #region Private Methods

#if !NETSTANDARD2_0
        private DynamicMethod CreateSetRefAsDynamicMethod(bool? generic)
        {
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            Debug.Assert(getterMethod.ReturnType.IsByRef);
            Debug.Assert(generic == null || ParameterTypes.Length == 1, "When creating a specialized delegate only 1 parameter is expected");

            Type propertyType = getterMethod.ReturnType.GetElementType()!;
            Type? declaringType = getterMethod.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

            Type[] parameterTypes =
            {
                generic == true ? (declaringType.IsValueType ? declaringType.MakeByRefType() : declaringType) : Reflector.ObjectType, // instance
                generic == true ? propertyType : Reflector.ObjectType, // value
                generic switch // indices/index
                {
                    false => Reflector.ObjectType,
                    true => ParameterTypes[0],
                    null => typeof(object[])
                }
            };

            var dm = new DynamicMethod("<SetRefIndexer>__" + Property.Name, Reflector.VoidType, parameterTypes, GetOwner(), true);

            ILGenerator ilGenerator = dm.GetILGenerator();

            // loading 0th argument (instance)
            Debug.Assert(!getterMethod.IsStatic, "Indexers are not expected to be static");
            ilGenerator.Emit(OpCodes.Ldarg_0);
            if (generic != true)
                ilGenerator.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);

            // assigning parameter(s)
            if (generic == null)
            {
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    Debug.Assert(!ParameterTypes[i].IsByRef, "Indexer parameters are never passed by reference");
                    ilGenerator.Emit(OpCodes.Ldarg_2); // loading 2nd argument (indices)
                    ilGenerator.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                    ilGenerator.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                    ilGenerator.Emit(ParameterTypes[i].IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, ParameterTypes[i]);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldarg_2);
                if (generic == false)
                    ilGenerator.Emit(ParameterTypes[0].IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, ParameterTypes[0]);
            }

            // calling the getter
            ilGenerator.Emit(getterMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getterMethod);

            // loading 1st argument (value)
            ilGenerator.Emit(OpCodes.Ldarg_1);
            if (generic != true)
                ilGenerator.Emit(propertyType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, propertyType);

            // setting the returned reference
            if (propertyType.IsValueType)
                ilGenerator.Emit(OpCodes.Stobj, propertyType);
            else
                ilGenerator.Emit(OpCodes.Stind_Ref);

            ilGenerator.Emit(OpCodes.Ret);
            return dm;
        }
#endif

        #endregion

        #endregion
    }
}
