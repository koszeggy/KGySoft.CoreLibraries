﻿#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FieldAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2022 - All Rights Reserved
//
//  You should have received a copy of the LICENSE file at the top-level
//  directory of this distribution.
//
//  Please refer to the LICENSE file if you want to use this source code.
///////////////////////////////////////////////////////////////////////////////

#endregion

#region Usings

using System.Linq;

#region Used Namespaces

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Collections.Generic;
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

using KGySoft.Annotations;
using KGySoft.CoreLibraries;

#endregion

#region Used Aliases

using NonGenericSetter = System.Action<object?, object?>;
using NonGenericGetter = System.Func<object?, object?>;

#endregion

#endregion

#region Suppressions

#if !(NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
#pragma warning disable CS8763 // A method marked [DoesNotReturn] should not return - false alarm, ExceptionDispatchInfo.Throw() does not return either.
#endif

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Provides an efficient way for setting and getting fields values via dynamically created delegates.
    /// <br/>See the <strong>Remarks</strong> section for details and an example.
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="FieldAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.</para>
    /// <para>The <see cref="Get">Get</see> and <see cref="Set">Set</see> methods can be used to get and set the field, respectively.
    /// The first call of these methods are slow because the delegates are generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to access a property by name rather then by a <see cref="FieldInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.SetField">SetField</see>
    /// and <see cref="O:KGySoft.Reflection.Reflector.SetField">GetField</see> methods in the <see cref="Reflector"/> class, which have some overloads with a <c>fieldName</c> parameter.</note>
    /// <note type="warning">The .NET Standard 2.0 version of the <see cref="Set">Set</see> method throws a <see cref="PlatformNotSupportedException"/>
    /// if the field to set is read-only or is an instance member of a value type (<see langword="struct"/>).
    /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
    /// <see cref="O:KGySoft.Reflection.Reflector.SetField">Reflector.SetField</see> methods to set read-only or value type instance fields.</note>
    /// </remarks>
    /// <example><code lang="C#"><![CDATA[
    /// using System;
    /// using System.Reflection;
    /// using KGySoft.Diagnostics;
    /// using KGySoft.Reflection;
    /// 
    /// class Example
    /// {
    ///     private class TestClass
    ///     {
    ///         public int TestField;
    ///     }
    /// 
    ///     static void Main(string[] args)
    ///     {
    ///         var instance = new TestClass();
    ///         FieldInfo field = instance.GetType().GetField(nameof(TestClass.TestField));
    ///         FieldAccessor accessor = FieldAccessor.GetAccessor(field);
    /// 
    ///         new PerformanceTest { TestName = "Set Field", Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestField = 1, "Direct set")
    ///             .AddCase(() => field.SetValue(instance, 1), "FieldInfo.SetValue")
    ///             .AddCase(() => accessor.Set(instance, 1), "FieldAccessor.Set")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    /// 
    ///         new PerformanceTest<int> { TestName = "Get Field", Iterations = 1000000 }
    ///             .AddCase(() => instance.TestField, "Direct get")
    ///             .AddCase(() => (int)field.GetValue(instance), "FieldInfo.GetValue")
    ///             .AddCase(() => (int)accessor.Get(instance), "FieldAccessor.Get")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // ==[Set Field Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 3
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct set: average time: 2.58 ms
    /// // 2. FieldAccessor.Set: average time: 10.92 ms (+8.34 ms / 422.84 %)
    /// // 3. FieldInfo.SetValue: average time: 110.98 ms (+108.40 ms / 4,296.20 %)
    /// // 
    /// // ==[Get Field Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 3
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct get: average time: 2.99 ms
    /// // 2. FieldAccessor.Get: average time: 8.56 ms (+5.58 ms / 286.69 %)
    /// // 3. FieldInfo.GetValue: average time: 111.37 ms (+108.38 ms / 3,729.24 %)]]></code>
    /// </example>
    public sealed class FieldAccessor : MemberAccessor
    {
        #region Constants

        private const string setterPrefix = "<SetField>__";

        #endregion

        #region Fields

        private NonGenericGetter? getter;
        private NonGenericSetter? setter;
        private Delegate? genericGetter;
        private Delegate? genericSetter;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets whether the field is read-only.
        /// </summary>
        /// <remarks>
        /// <note>Even if this property returns <see langword="true"/>&#160;the <see cref="FieldAccessor"/>
        /// is able to set the field, except if the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly is used,
        /// which throws a <see cref="PlatformNotSupportedException"/> in that case.</note>
        /// </remarks>
        public bool IsReadOnly => ((FieldInfo)MemberInfo).IsInitOnly;

        /// <summary>
        /// Gets whether the field is a constant. Constant fields cannot be set.
        /// </summary>
        public bool IsConstant => ((FieldInfo)MemberInfo).IsLiteral;

        #endregion

        #region Private Properties

        private FieldInfo Field => (FieldInfo)MemberInfo;
        private NonGenericGetter Getter => getter ??= CreateGetter();
        private NonGenericSetter Setter => setter ??= CreateSetter();
        private Delegate GenericGetter => genericGetter ??= CreateGenericGetter();
        private Delegate GenericSetter => genericSetter ??= CreateGenericSetter();

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldAccessor"/> class.
        /// </summary>
        /// <param name="field">The field for which the accessor is to be created.</param>
        private FieldAccessor(FieldInfo field) : base(field, null)
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="FieldAccessor"/> for the specified <paramref name="field"/>.
        /// </summary>
        /// <param name="field">The field for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="FieldAccessor"/> instance that can be used to get or set the field.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static FieldAccessor GetAccessor(FieldInfo field)
        {
            if (field == null!)
                Throw.ArgumentNullException(Argument.field);
            return (FieldAccessor)GetCreateAccessor(field);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates an accessor for a field without caching.
        /// </summary>
        /// <param name="field">The field for which an accessor should be created.</param>
        /// <returns>A <see cref="FieldAccessor"/> instance that can be used to get or set the field.</returns>
        internal static FieldAccessor CreateAccessor(FieldInfo field) => new FieldAccessor(field);

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Sets the field.
        /// For static fields the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="instance">The instance that the field belongs to. Can be <see langword="null"/>&#160;for static fields.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// <para>Setting the field for the first time is slower than the <see cref="FieldInfo.SetValue(object,object)">System.Reflection.FieldInfo.SetValue</see>
        /// method but further calls are much faster.</para>
        /// <note type="caller">Calling the .NET Standard 2.0 version of this method throws a <see cref="PlatformNotSupportedException"/>
        /// if the field to set is read-only or is an instance member of a value type (<see langword="struct"/>).
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.SetField">Reflector.SetField</see> methods to set read-only or value type instance fields.</note>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public void Set(object? instance, object? value)
        {
            try
            {
                // For the best performance not validating the arguments in advance
                Setter.Invoke(instance, value);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception
                PostValidate(instance, value, e, true);
            }
        }

        /// <summary>
        /// Gets the value of the field.
        /// For static fields the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// <br/>See the <strong>Remarks</strong> section for details.
        /// </summary>
        /// <param name="instance">The instance that the field belongs to. Can be <see langword="null"/>&#160;for static fields.</param>
        /// <returns>The value of the field.</returns>
        /// <remarks>
        /// <para>Getting the field for the first time is slower than the <see cref="FieldInfo.GetValue">System.Reflection.FieldInfo.GetValue</see>
        /// method but further calls are much faster.</para>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object? Get(object? instance)
        {
            try
            {
                // For the best performance not validating the arguments in advance
                return Getter.Invoke(instance);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception
                PostValidate(instance, null, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetStaticValue<TField>(TField value)
        {
            if (GenericSetter is Action<TField> action)
                action.Invoke(value);
            else
                ThrowStatic<TField>();
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TField GetStaticValue<TField>() => GenericGetter is Func<TField> func ? func.Invoke() : ThrowStatic<TField>();

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void SetInstanceValue<TInstance, TField>(TInstance instance, TField value) where TInstance : class
        {
            if (GenericSetter is ReferenceTypeAction<TInstance, TField> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), value);
            else
                ThrowInstance<TField>();
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TField GetInstanceValue<TInstance, TField>(TInstance instance) where TInstance : class
            => GenericGetter is ReferenceTypeFunction<TInstance, TField> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance))
                : ThrowInstance<TField>();

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetInstanceValue<TInstance, TField>(in TInstance instance, TField value) where TInstance : struct
        {
            if (GenericSetter is ValueTypeAction<TInstance, TField> action)
                action.Invoke(instance, value);
            else
                ThrowInstance<TField>();
        }

        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TField GetInstanceValue<TInstance, TField>(in TInstance instance) where TInstance : struct
            => GenericGetter is ValueTypeFunction<TInstance, TField> func ? func.Invoke(instance) : ThrowInstance<TField>();

        #endregion

        #region Private Methods

        private NonGenericGetter CreateGetter()
        {
            Type? declaringType = Field.DeclaringType;
            if (!Field.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Field.FieldType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            MemberExpression member = Expression.Field(
                    // ReSharper disable once AssignNullToNotNullAttribute - the check above prevents null
                    Field.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                    Field);

            Expression<NonGenericGetter> lambda = Expression.Lambda<NonGenericGetter>(
                    Expression.Convert(member, Reflector.ObjectType), // object return type
                    instanceParameter); // instance (object)
            return lambda.Compile();
        }

        private NonGenericSetter CreateSetter()
        {
            Type? declaringType = Field.DeclaringType;
            if (IsConstant)
                Throw.InvalidOperationException(Res.ReflectionCannotSetConstantField(MemberInfo.DeclaringType, MemberInfo.Name));
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Field.FieldType));

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            if (Field.IsInitOnly)
                Throw.PlatformNotSupportedException(Res.ReflectionSetReadOnlyFieldNetStandard20(Field.Name, declaringType));
            if (!Field.IsStatic && declaringType.IsValueType)
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructFieldNetStandard20(Field.Name, declaringType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            UnaryExpression castValue = Expression.Convert(valueParameter, Field.FieldType);

            MemberExpression member = Expression.Field(
                Field.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                Field);

            BinaryExpression assign = Expression.Assign(member, castValue);
            Expression<NonGenericSetter> lambda = Expression.Lambda<NonGenericSetter>(
                assign,
                instanceParameter, // instance (object)
                valueParameter);
            return lambda.Compile();
#else
            // Expressions would not work for value types and read-only fields so using always dynamic methods
            DynamicMethod dm = new DynamicMethod(setterPrefix + Field.Name, // setter method name
                Reflector.VoidType, // return type
                new[] { Reflector.ObjectType, Reflector.ObjectType }, declaringType, true); // instance and value parameters

            ILGenerator il = dm.GetILGenerator();

            // if instance field, then processing instance parameter
            if (!Field.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                il.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType); // casting object instance to target type
            }

            il.Emit(OpCodes.Ldarg_1); // loading 1st argument: value parameter
            il.Emit(Field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, Field.FieldType); // casting object value to field type
            il.Emit(Field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, Field); // processing assignment
            il.Emit(OpCodes.Ret); // returning without return value

            return (NonGenericSetter)dm.CreateDelegate(typeof(NonGenericSetter));
#endif
        }

        private Delegate CreateGenericGetter()
        {
            Type? declaringType = Field.DeclaringType;
            if (!Field.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Field.FieldType));

            MemberExpression member;
            ParameterExpression instanceParameter;
            LambdaExpression lambda;

            // Static field
            if (Field.IsStatic)
            {
                member = Expression.Field(null, Field);
                lambda = Expression.Lambda(typeof(Func<>).GetGenericType(Field.FieldType), member);
                return lambda.Compile();
            }

            // Class instance field
            if (!declaringType!.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                member = Expression.Field(instanceParameter, Field);
                lambda = Expression.Lambda(typeof(ReferenceTypeFunction<,>).GetGenericType(declaringType, Field.FieldType), member, instanceParameter);
                return lambda.Compile();
            }

            // Struct instance field
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            member = Expression.Field(instanceParameter, Field);
            lambda = Expression.Lambda(typeof(ValueTypeFunction<,>).GetGenericType(declaringType, Field.FieldType), member, instanceParameter);
            return lambda.Compile();
        }

        private Delegate CreateGenericSetter()
        {
            Type? declaringType = Field.DeclaringType;
            if (IsConstant)
                Throw.InvalidOperationException(Res.ReflectionCannotSetConstantField(MemberInfo.DeclaringType, MemberInfo.Name));
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Field.FieldType));

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            if (Field.IsInitOnly)
                Throw.PlatformNotSupportedException(Res.ReflectionSetReadOnlyFieldNetStandard20(Field.Name, declaringType));

            ParameterExpression instanceParameter;
            ParameterExpression valueParameter = Expression.Parameter(Field.FieldType, "value");
            MemberExpression member;
            BinaryExpression assign;
            LambdaExpression lambda;

            // Static field
            if (Field.IsStatic)
            {
                member = Expression.Field(null, Field);
                assign = Expression.Assign(member, valueParameter);
                lambda = Expression.Lambda(typeof(Action<>).GetGenericType(Field.FieldType), assign, valueParameter);
                return lambda.Compile();
            }

            // Class instance field
            if (!declaringType.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                member = Expression.Field(instanceParameter, Field);
                assign = Expression.Assign(member, valueParameter);
                lambda = Expression.Lambda(typeof(ReferenceTypeAction<,>).GetGenericType(declaringType, Field.FieldType), assign, instanceParameter, valueParameter);
                return lambda.Compile();
            }

            // Struct instance field
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            member = Expression.Field(instanceParameter, Field);
            assign = Expression.Assign(member, valueParameter);
            lambda = Expression.Lambda(typeof(ValueTypeAction<,>).GetGenericType(declaringType, Field.FieldType), assign, instanceParameter, valueParameter);
            return lambda.Compile();
#else
            // Expressions would not work for read-only fields so using always dynamic methods
            Type[] parameterTypes = (Field.IsStatic ? Type.EmptyTypes : new[] { declaringType.IsValueType ? declaringType.MakeByRefType() : declaringType })
                .Concat(new[] { Field.FieldType })
                .ToArray();

            var dm = new DynamicMethod(setterPrefix + Field.Name, Reflector.VoidType, parameterTypes, declaringType, true);
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // loading 0th argument: instance for instance fields, value for static fields
            if (Field.IsStatic)
                il.Emit(OpCodes.Stsfld, Field); // assigning static field
            else
            {
                il.Emit(OpCodes.Ldarg_1); // loading 1st argument: value parameter for instance fields
                il.Emit(OpCodes.Stfld, Field); // assigning instance field
            }

            il.Emit(OpCodes.Ret); // returning without return value
            Type delegateType = Field.IsStatic ? typeof(Action<>).GetGenericType(parameterTypes)
                : declaringType.IsValueType ? typeof(ValueTypeAction<,>).GetGenericType(declaringType, Field.FieldType)
                : typeof(ReferenceTypeAction<,>).GetGenericType(parameterTypes);
            return dm.CreateDelegate(delegateType);
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ContractAnnotation("=> halt"), DoesNotReturn]
        private void PostValidate(object? instance, object? value, Exception exception, bool isSetter)
        {
            if (!Field.IsStatic)
            {
                if (instance == null)
                    Throw.ArgumentNullException(Argument.instance, Res.ReflectionInstanceIsNull);
                if (!Field.DeclaringType!.CanAcceptValue(instance))
                    Throw.ArgumentException(Argument.instance, Res.NotAnInstanceOfType(Field.DeclaringType!));
            }

            if (isSetter)
            {
                if (!Field.FieldType.CanAcceptValue(value))
                {
                    if (value == null)
                        Throw.ArgumentNullException(Argument.value, Res.NotAnInstanceOfType(Field.FieldType));
                    Throw.ArgumentException(Argument.value, Res.NotAnInstanceOfType(Field.FieldType));
                }
            }

            ThrowIfSecurityConflict(exception, isSetter ? setterPrefix : null);

            // exceptions from the property itself: re-throwing the original exception
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T ThrowStatic<T>() => !Field.IsStatic
            ? Throw.InvalidOperationException<T>(Res.ReflectionStaticFieldExpectedGeneric(Field.Name, Field.DeclaringType!))
            : Throw.ArgumentException<T>(Res.ReflectionCannotInvokeFieldGeneric(Field.Name, Field.DeclaringType));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T ThrowInstance<T>() => Field.IsStatic
            ? Throw.InvalidOperationException<T>(Res.ReflectionInstanceFieldExpectedGeneric(Field.Name, Field.DeclaringType))
            : Throw.ArgumentException<T>(Res.ReflectionCannotInvokeFieldGeneric(Field.Name, Field.DeclaringType));

        #endregion

        #endregion

        #endregion
    }
}
