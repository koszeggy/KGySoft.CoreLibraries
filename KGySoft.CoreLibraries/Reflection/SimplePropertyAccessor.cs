#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: SimplePropertyAccessor.cs
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

#region Used Namespaces

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;

using KGySoft.CoreLibraries;

#endregion

#region Used Aliases

using NonGenericSetter = System.Action<object?, object?>;
using NonGenericGetter = System.Func<object?, object?>;

#endregion

#endregion

namespace KGySoft.Reflection
{
    internal sealed class SimplePropertyAccessor : PropertyAccessor
    {
        #region Constructors

        internal SimplePropertyAccessor(PropertyInfo pi)
            : base(pi)
        {
        }

        #endregion

        #region Methods

        #region Public Methods

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public override void Set(object? instance, object? value, params object?[]? indexParameters)
        {
            try
            {
                // For the best performance not validating the arguments in advance
                ((NonGenericSetter)Setter).Invoke(instance, value);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception
                PostValidate(instance, value, indexParameters, e, true);
            }
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public override object? Get(object? instance, params object?[]? indexParameters)
        {
            try
            {
                // For the best performance not validating the arguments in advance
                return ((NonGenericGetter)Getter).Invoke(instance);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception
                PostValidate(instance, null, indexParameters, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        #endregion

        #region Private Protected Methods

        private protected override Delegate CreateGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (!getterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

#if !NETSTANDARD2_0
            // for struct instance properties: Dynamic method
            if (declaringType!.IsValueType && !getterMethod.IsStatic)
            {
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(getterMethod, DynamicMethodOptions.OmitParameters);
                return dm.CreateDelegate(typeof(NonGenericGetter));
            }
#endif

            // For classes and static properties: Lambda expression (.NET Standard 2.0: also for structs, mutated content might be lost)
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");

            MemberExpression member = Expression.Property(
                getterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                Property);

            LambdaExpression lambda = Expression.Lambda<NonGenericGetter>(
                Expression.Convert(member, Reflector.ObjectType), // object return type
                instanceParameter); // instance (object)
            return lambda.Compile();
        }

        private protected override Delegate CreateSetter()
        {
            if (!CanWrite)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
            MethodInfo setterMethod = Property.GetSetMethod(true)!;
            Type? declaringType = setterMethod.DeclaringType;
            if (!setterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            if (declaringType!.IsValueType && !setterMethod.IsStatic)
            {
#if NETSTANDARD2_0
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructPropertyNetStandard20(Property.Name, declaringType));
#else
                // for struct instance properties: Dynamic method
                DynamicMethod dm = CreateMethodInvokerAsDynamicMethod(setterMethod, DynamicMethodOptions.TreatAsPropertySetter);
                return dm.CreateDelegate(typeof(NonGenericSetter));
#endif
            }

            // for classes and static properties: Lambda expression
            // Calling the setter method (works even in .NET 3.5, while Assign is available from .NET 4 only)
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            UnaryExpression castValue = Expression.Convert(valueParameter, Property.PropertyType);

            MethodCallExpression setterCall = Expression.Call(
                setterMethod.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                setterMethod, // setter
                castValue); // original parameter: (TProp)value

            LambdaExpression lambda = Expression.Lambda<NonGenericSetter>(
                setterCall, // no return type
                instanceParameter, // instance (object)
                valueParameter);
            return lambda.Compile();
        }

        private protected override Delegate CreateGenericGetter()
        {
            if (!CanRead)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));

            MethodInfo getterMethod = Property.GetGetMethod(true)!;
            Type? declaringType = getterMethod.DeclaringType;
            if (!getterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            MethodCallExpression getterCall;
            ParameterExpression instanceParameter;
            LambdaExpression lambda;

            // Static property
            if (getterMethod.IsStatic)
            {
                getterCall = Expression.Call(null, getterMethod);
                lambda = Expression.Lambda(typeof(Func<>).GetGenericType(Property.PropertyType), getterCall);
                return lambda.Compile();
            }

            // Class instance property
            if (!declaringType!.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                getterCall = Expression.Call(instanceParameter, getterMethod);
                lambda = Expression.Lambda(typeof(ReferenceTypeFunction<,>).GetGenericType(declaringType, Property.PropertyType), getterCall, instanceParameter);
                return lambda.Compile();
            }

#if NET35
            // Expression.Call fails for .NET Framework 3.5 if the instance is a ByRef type so using DynamicMethod instead
            var dm = new DynamicMethod("<GetProperty>__" + Property.Name, Property.PropertyType,
                new[] { declaringType.MakeByRefType() }, declaringType, true);
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // loading 0th argument: instance
            il.Emit(getterMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getterMethod); // calling the getter
            il.Emit(OpCodes.Ret); // return
            return dm.CreateDelegate(typeof(ValueTypeFunction<,>).GetGenericType(declaringType, Property.PropertyType));
#else
            // Struct instance property
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            getterCall = Expression.Call(instanceParameter, getterMethod);
            lambda = Expression.Lambda(typeof(ValueTypeFunction<,>).GetGenericType(declaringType, Property.PropertyType), getterCall, instanceParameter);
            return lambda.Compile();
#endif
        }

        private protected override Delegate CreateGenericSetter()
        {
            if (!CanWrite)
                Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));

            MethodInfo setterMethod = Property.GetSetMethod(true)!;
            Type? declaringType = setterMethod.DeclaringType;
            if (!setterMethod.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Property.PropertyType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Property.PropertyType));

            ParameterExpression instanceParameter;
            ParameterExpression valueParameter = Expression.Parameter(Property.PropertyType, "value");
            MethodCallExpression setterCall;
            LambdaExpression lambda;

            // Static property
            if (setterMethod.IsStatic)
            {
                setterCall = Expression.Call(null, setterMethod, valueParameter);
                lambda = Expression.Lambda(typeof(Action<>).GetGenericType(Property.PropertyType), setterCall, valueParameter);
                return lambda.Compile();
            }

            // Class instance property
            if (!declaringType!.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                setterCall = Expression.Call(instanceParameter, setterMethod, valueParameter);
                lambda = Expression.Lambda(typeof(ReferenceTypeAction<,>).GetGenericType(declaringType, Property.PropertyType), setterCall, instanceParameter, valueParameter);
                return lambda.Compile();
            }

#if NET35
            // Expression.Call fails for .NET Framework 3.5 if the instance is a ByRef type so using DynamicMethod instead
            var dm = new DynamicMethod("<SetProperty>__" + Property.Name, Reflector.VoidType,
                new[] { declaringType.MakeByRefType(), Property.PropertyType }, declaringType, true);
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // loading 0th argument: instance
            il.Emit(OpCodes.Ldarg_1); // loading 1st argument: value
            il.Emit(setterMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setterMethod); // calling the setter
            il.Emit(OpCodes.Ret); // return
            return dm.CreateDelegate(typeof(ValueTypeAction<,>).GetGenericType(declaringType, Property.PropertyType));
#else
            // Struct instance property
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            setterCall = Expression.Call(instanceParameter, setterMethod, valueParameter);
            lambda = Expression.Lambda(typeof(ValueTypeAction<,>).GetGenericType(declaringType, Property.PropertyType), setterCall, instanceParameter, valueParameter);
            return lambda.Compile();
#endif
        }

        #endregion

        #endregion
    }
}
