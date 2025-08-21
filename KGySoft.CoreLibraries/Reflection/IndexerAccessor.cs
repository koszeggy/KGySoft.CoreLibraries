#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: IndexerAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2024 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System;
#if NETSTANDARD2_0
using System.Linq;
using System.Linq.Expressions;
#endif
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
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);

            if (!Property.CanWrite)
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
            // Value type: using reflection as fallback so mutations are preserved. Same for pointer property value or parameter that are not supported by Expression trees.
            if (declaringType.IsValueType && !(declaringType.IsReadOnly() || setterMethod.IsReadOnly())
                || setterMethod.GetParameters().Any(p => p.ParameterType.IsPointer)) // no need to check the property type, it is the same as the last parameter type in setterMethod
            {
                return Property.SetValue;
            }

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
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;

#if NETSTANDARD2_0
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            // Non-readonly value type: using reflection as fallback so mutations are preserved. Same for pointer properties or pointer index parameters that are not supported by Expression trees.
            if (declaringType.IsValueType && !(declaringType.IsReadOnly() || getterMethod.IsReadOnly())
                || Property.PropertyType.IsPointer || getterMethod.GetParameters().Any(p => p.ParameterType.IsPointer))
            {
                unsafe
                {
                    return Property.PropertyType.IsPointer
                        ? (instance, indexParams) => (IntPtr)Pointer.Unbox(Property.GetValue(instance, indexParams))
                        : Property.GetValue;
                }
            }

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
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);

            // The 1 parameter overload was called for a more-params indexer
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(); // Will be handled in PostValidate

            if (!Property.CanWrite)
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
            // Value type: using reflection as fallback so mutations are preserved. Same for pointer property value or parameter that are not supported by Expression trees.
            if (declaringType.IsValueType && !(declaringType.IsReadOnly() || setterMethod.IsReadOnly())
                || setterMethod.GetParameters().Any(p => p.ParameterType.IsPointer)) // no need to check the property type, it is the same as the last parameter type in setterMethod
            {
                return new Action<object?, object?, object?>((o, v, i) => Property.SetValue(o, v, [i]));
            }

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
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));

            // The 1 parameter overload was called for a more-params indexer
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(); // Will be handled in PostValidate

            MethodInfo getterMethod = Property.GetGetMethod(true)!;

#if NETSTANDARD2_0
            if (Property.PropertyType.IsByRef)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            // Non-readonly value type: using reflection as fallback so mutations are preserved. Same for pointer properties or pointer index parameters that are not supported by Expression trees.
            if (declaringType.IsValueType && !(declaringType.IsReadOnly() || getterMethod.IsReadOnly())
                || Property.PropertyType.IsPointer || ParameterTypes[0].IsPointer)
            {
                unsafe
                {
                    return Property.PropertyType.IsPointer
                        ? (instance, index) => (IntPtr)Pointer.Unbox(Property.GetValue(instance, [index]))
                        : new Func<object?, object?, object?>((instance, index) => Property.GetValue(instance, [index]));
                }
            }

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
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(Res.ReflectionIndexerGenericNotSupported);

            bool isByRef = Property.PropertyType.IsByRef;
            bool isValueType = declaringType.IsValueType;
            Type propertyType = isByRef ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            if (propertyType.IsPointer)
                propertyType = typeof(IntPtr);
            Type indexType = ParameterTypes[0];
            if (indexType.IsPointer)
                indexType = typeof(IntPtr);
            Type delegateType = (isValueType ? typeof(ValueTypeAction<,,>) : typeof(ReferenceTypeAction<,,>))
                .GetGenericType(declaringType, propertyType, indexType);

            if (!Property.CanWrite)
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
            ParameterExpression indexParameter = Expression.Parameter(indexType, "index");
            ParameterExpression valueParameter = Expression.Parameter(propertyType, "value");
            LambdaExpression lambda;

            // Pointer property: fallback to PropertyInfo.SetValue(object,object,object[]), which supports pointers as IntPtr
            if (ParameterTypes.Any(p => p.IsPointer))
            {
                // value types: though we can call SetValue(object,object,object[]), the ref instance parameter gets boxed in a new object, losing all mutations
                if (isValueType && !declaringType.IsReadOnly() && !setterMethod.IsReadOnly())
                    Throw.PlatformNotSupportedException(Res.ReflectionValueTypeWithPointersGenericNetStandard20);

                Expression[] methodParameters =
                [
                    Expression.Convert(instanceParameter, Reflector.ObjectType), // instance
                    Expression.Convert(valueParameter, Reflector.ObjectType), // value
                    Expression.NewArrayInit(Reflector.ObjectType, Expression.Convert(indexParameter, Reflector.ObjectType)) // index as object[]
                ];

                MethodCallExpression methodCall = Expression.Call(
                    Expression.Constant(Property), // the instance is the PropertyInfo itself
                    Property.GetType().GetMethod(nameof(PropertyInfo.SetValue), [typeof(object), typeof(object), typeof(object[])])!, // SetValue(object, object, object[])
                    methodParameters);

                lambda = Expression.Lambda(delegateType, methodCall, instanceParameter, valueParameter, indexParameter);
                return lambda.Compile();
            }

            // note that in the setter method the index comes first, then the value to set (as opposed to PropertyInfo.SetValue where the value comes first)
            MethodCallExpression setterCall = Expression.Call(instanceParameter, setterMethod, indexParameter, valueParameter);
            lambda = Expression.Lambda(delegateType, setterCall, instanceParameter, valueParameter, indexParameter);
            return lambda.Compile();
#else
            DynamicMethod result = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter | DynamicMethodOptions.ExactParameters | DynamicMethodOptions.StronglyTyped);
            return result.CreateDelegate(delegateType);
#endif
        }

        private protected override Delegate CreateGenericGetter()
        {
            Type? declaringType = Property.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (declaringType.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            if (ParameterTypes.Length > 1)
                Throw.NotSupportedException(Res.ReflectionIndexerGenericNotSupported);

            bool isValueType = declaringType.IsValueType;
            bool isRefReturn = Property.PropertyType.IsByRef;
            Type returnType = isRefReturn ? Property.PropertyType.GetElementType()! : Property.PropertyType;
            if (returnType.IsPointer)
                returnType = typeof(IntPtr);
            Type indexType = ParameterTypes[0];
            if (indexType.IsPointer)
                indexType = typeof(IntPtr);
            Type delegateType = (isValueType ? typeof(ValueTypeFunction<,,>) : typeof(ReferenceTypeFunction<,,>))
                .GetGenericType(declaringType, indexType, returnType);

#if NETSTANDARD2_0
            if (isRefReturn)
                Throw.PlatformNotSupportedException(Res.ReflectionRefReturnTypeNetStandard20(Property.PropertyType));

            ParameterExpression instanceParameter = Expression.Parameter(isValueType ? declaringType.MakeByRefType() : declaringType, "instance");
            ParameterExpression indexParameter = Expression.Parameter(indexType, "index");
            LambdaExpression lambda;

            // Pointer property: fallback to NonGenericGetter.Invoke(object,object), which supports pointers as IntPtr.
            // NOTE: Unlike in the setter, we cannot use PropertyInfo.GetValue(object,object[]) here, because we should call Pointer.Unbox(object) on the result,
            // which is not possible by Expression trees.
            if (Property.PropertyType.IsPointer || ParameterTypes[0].IsPointer)
            {
                // value types: though we can call NonGenericGetter.Invoke(object), the ref instance parameter gets boxed in a new object, losing all mutations
                if (isValueType && !declaringType.IsReadOnly() && !getterMethod.IsReadOnly())
                    Throw.PlatformNotSupportedException(Res.ReflectionValueTypeWithPointersGenericNetStandard20);

                Expression[] methodParameters =
                [
                    Expression.Convert(instanceParameter, Reflector.ObjectType), // instance
                    Expression.Convert(indexParameter, Reflector.ObjectType) // index
                ];

                MethodCallExpression methodCall = Expression.Call(
                    Expression.Constant(NonGenericGetter),
                    NonGenericGetter.GetType().GetMethod("Invoke", [typeof(object), typeof(object)])!,
                    methodParameters);

                lambda = Expression.Lambda(delegateType, Expression.Convert(methodCall, returnType), instanceParameter, indexParameter);
                return lambda.Compile();
            }

            // Note: Expression.Call works everywhere but .NET Framework 3.5 if the instance is a ByRef type
            MethodCallExpression getterCall = Expression.Call(instanceParameter, getterMethod, indexParameter);
            lambda = Expression.Lambda(delegateType, getterCall, instanceParameter, indexParameter);
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
            Type? declaringType = getterMethod.DeclaringType;
            Debug.Assert(getterMethod.ReturnType.IsByRef);
            Debug.Assert(generic == null || ParameterTypes.Length == 1, "When creating a specialized delegate only 1 parameter is expected");
            Debug.Assert(declaringType != null);

            Type propertyType = getterMethod.ReturnType.GetElementType()!;
            bool isPointer = propertyType.IsPointer;
            Type valueParameterType = isPointer ? typeof(IntPtr) : propertyType;

            Type[] parameterTypes =
            {
                generic == true ? (declaringType!.IsValueType ? declaringType.MakeByRefType() : declaringType) : Reflector.ObjectType, // instance
                generic == true ? valueParameterType : Reflector.ObjectType, // value
                generic switch // indices/index
                {
                    false => Reflector.ObjectType,
                    true => ParameterTypes[0].IsPointer ? typeof(IntPtr) : ParameterTypes[0],
                    null => typeof(object[])
                }
            };

            var dm = new DynamicMethod("<SetRefIndexer>__" + Property.Name, Reflector.VoidType, parameterTypes, GetOwner(), true);

            ILGenerator ilGenerator = dm.GetILGenerator();

            // loading 0th argument (instance)
            Debug.Assert(!getterMethod.IsStatic, "Indexers are not expected to be static");
            ilGenerator.Emit(OpCodes.Ldarg_0);
            if (generic != true)
                ilGenerator.Emit(declaringType!.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType);

            // assigning parameter(s)
            if (generic == null)
            {
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    Debug.Assert(!ParameterTypes[i].IsByRef, "Indexer parameters are never passed by reference");
                    Type paramType = ParameterTypes[i];
                    if (paramType.IsPointer)
                        paramType = typeof(IntPtr);
                    ilGenerator.Emit(OpCodes.Ldarg_2); // loading 2nd argument (indices)
                    ilGenerator.Emit(OpCodes.Ldc_I4, i); // loading index of processed argument
                    ilGenerator.Emit(OpCodes.Ldelem_Ref); // loading the pointed element in arguments
                    ilGenerator.Emit(paramType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, paramType);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldarg_2);
                Type indexType = ParameterTypes[0];
                if (indexType.IsPointer)
                    indexType = typeof(IntPtr);
                if (generic == false)
                    ilGenerator.Emit(indexType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, indexType);
            }

            // calling the getter
            ilGenerator.Emit(getterMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getterMethod);

            // loading 1st argument (value)
            ilGenerator.Emit(OpCodes.Ldarg_1);
            if (generic != true)
                ilGenerator.Emit(valueParameterType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, valueParameterType);

            // setting the returned reference
            if (isPointer)
                ilGenerator.Emit(OpCodes.Stind_I);
            else if (propertyType.IsValueType)
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
