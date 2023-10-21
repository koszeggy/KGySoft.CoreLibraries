#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FieldAccessor.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Reflection.Emit;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

using KGySoft.Annotations;
using KGySoft.CoreLibraries;

#endregion

#region Suppressions

#if !(NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
#pragma warning disable CS8763 // A method marked [DoesNotReturn] should not return - false alarm, ExceptionDispatchInfo.Throw() does not return either.
#endif

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Provides an efficient way for setting and getting field values via dynamically created delegates.
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="FieldAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.</para>
    /// <para>The <see cref="Get">Get</see> and <see cref="Set">Set</see> methods can be used to get and set the field, respectively.</para>
    /// <para>If you know the field type at compile time, then you can use the generic <see cref="SetStaticValue{TField}">SetStaticValue</see>/<see cref="GetStaticValue{TField}">GetStaticValue</see>
    /// methods for static fields. If you know also the instance type, then
    /// the <see cref="O:KGySoft.Reflection.FieldAccessor.GetInstanceValue">GetInstanceValue</see>/<see cref="O:KGySoft.Reflection.FieldAccessor.SetInstanceValue">SetInstanceValue</see>
    /// methods can be used for instance field for better performance.</para>
    /// <para>The first call of these methods are slow because the delegates are generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to access a field by name rather than by a <see cref="FieldInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.SetField">SetField</see>
    /// and <see cref="O:KGySoft.Reflection.Reflector.SetField">GetField</see> methods in the <see cref="Reflector"/> class, which have some overloads with a <c>fieldName</c> parameter.</note>
    /// <note type="warning">The .NET Standard 2.0 version of the <see cref="Set">Set</see> method throws a <see cref="PlatformNotSupportedException"/>
    /// if the field to set is read-only or is an instance member of a value type (<see langword="struct"/>).
    /// The generic <see cref="O:KGySoft.Reflection.FieldAccessor.SetInstanceValue">SetInstanceValue</see> methods also throw a <see cref="PlatformNotSupportedException"/>
    /// for read-only fields when using the .NET Standard 2.0 build of the libraries, though they support non read-only value type fields.
    /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
    /// <see cref="O:KGySoft.Reflection.Reflector.SetField">Reflector.SetField</see> methods to set read-only fields or value type instance fields in a non-generic way.</note>
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
    ///             .AddCase(() => accessor.SetInstanceValue(instance, 1), "FieldAccessor.SetInstanceValue<,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    /// 
    ///         new PerformanceTest<int> { TestName = "Get Field", Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestField, "Direct get")
    ///             .AddCase(() => (int)field.GetValue(instance), "FieldInfo.GetValue")
    ///             .AddCase(() => (int)accessor.Get(instance), "FieldAccessor.Get")
    ///             .AddCase(() => accessor.GetInstanceValue<TestClass, int>(instance), "FieldAccessor.GetInstanceValue<,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // ==[Set Field Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct set: average time: 2.79 ms
    /// // 2. FieldAccessor.SetInstanceValue<,>: average time: 7.51 ms (+4.72 ms / 269.41%)
    /// // 3. FieldAccessor.Set: average time: 10.20 ms(+7.42 ms / 366.09%)
    /// // 4. FieldInfo.SetValue: average time: 61.40 ms(+58.62 ms / 2,202.91%)
    /// // 
    /// // ==[Get Field Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct get: average time: 2.08 ms
    /// // 2. FieldAccessor.GetInstanceValue<,>: average time: 4.84 ms (+2.77 ms / 233.33%)
    /// // 3. FieldAccessor.Get: average time: 8.06 ms(+5.98 ms / 388.12%)
    /// // 4. FieldInfo.GetValue: average time: 50.32 ms(+48.24 ms / 2,423.40%)]]></code>
    /// </example>
    public sealed class FieldAccessor : MemberAccessor
    {
        #region Constants

        private const string getterPrefix = "<GetField>__";
        private const string setterPrefix = "<SetField>__";

        #endregion

        #region Fields

        private Action<object?, object?>? setter;
        private Func<object?, object?>? getter;
        private Delegate? genericSetter;
        private Delegate? genericGetter;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets whether the field is read-only.
        /// </summary>
        /// <remarks>
        /// <note>Even if this property returns <see langword="true"/> the <see cref="FieldAccessor"/>
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
        private Action<object?, object?> Setter => setter ??= CreateSetter();
        private Func<object?, object?> Getter => getter ??= CreateGetter();
        private Delegate GenericSetter => genericSetter ??= CreateGenericSetter();
        private Delegate GenericGetter => genericGetter ??= CreateGenericGetter();

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
        /// </summary>
        /// <param name="instance">The instance that the field belongs to. Can be <see langword="null"/> for static fields.</param>
        /// <param name="value">The value to set.</param>
        /// <remarks>
        /// <para>Setting the field for the first time is slower than the <see cref="FieldInfo.SetValue(object,object)">System.Reflection.FieldInfo.SetValue</see>
        /// method but further calls are much faster.</para>
        /// <note type="tip">If you know the type of the field at compile time (and also the declaring type for instance fields),
        /// then you can use the generic <see cref="SetStaticValue{TField}">SetStaticValue</see>
        /// or <see cref="O:KGySoft.Reflection.FieldAccessor.SetInstanceValue">SetInstanceValue</see> methods for better performance.</note>
        /// <note type="caller">Calling the .NET Standard 2.0 version of this method throws a <see cref="PlatformNotSupportedException"/>
        /// if the field to set is read-only or is an instance member of a value type (<see langword="struct"/>).
        /// The <see cref="O:KGySoft.Reflection.FieldAccessor.SetInstanceValue">SetInstanceValue</see> methods support setting value fields though.
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.SetField">Reflector.SetField</see> methods to set read-only fields or value type instance fields in a non-generic way.</note>
        /// </remarks>
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant field.</exception>
        /// <exception cref="ArgumentNullException">This <see cref="FieldAccessor"/> represents an instance field and <paramref name="instance"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/>This <see cref="FieldAccessor"/> represents a value type field and <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> or <paramref name="value"/> is invalid.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="FieldAccessor"/>
        /// represents a read-only field or its declaring type is a value type.</exception>
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
        /// </summary>
        /// <param name="instance">The instance that the field belongs to. Can be <see langword="null"/> for static fields.</param>
        /// <returns>The value of the field.</returns>
        /// <remarks>
        /// <para>Getting the field for the first time is slower than the <see cref="FieldInfo.GetValue">System.Reflection.FieldInfo.GetValue</see>
        /// method but further calls are much faster.</para>
        /// <note type="tip">If you know the type of the field at compile time (and also the declaring type for instance fields),
        /// then you can use the generic <see cref="GetStaticValue{TField}">GetStaticValue</see>
        /// or <see cref="O:KGySoft.Reflection.FieldAccessor.GetInstanceValue">GetInstanceValue</see> methods for better performance.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">This <see cref="FieldAccessor"/> represents an instance field and <paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> is invalid.</exception>
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

        /// <summary>
        /// Sets the strongly typed value of a static field. If the type of the field is not known at compile time
        /// the non-generic <see cref="Set">Set</see> method can be used.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="value">The value to set.</param>
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant or an instance field.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="TField"/> is invalid.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c>
        /// and this <see cref="FieldAccessor"/> represents a read-only field.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetStaticValue<TField>(TField value)
        {
            if (GenericSetter is Action<TField> action)
                action.Invoke(value);
            else
                ThrowStatic<TField>();
        }

        /// <summary>
        /// Gets the strongly typed value of a static field. If the type of the field is not known at compile time
        /// the non-generic <see cref="Get">Get</see> method can be used.
        /// </summary>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <returns>The value of the field.</returns>
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents an instance field.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="TField"/> is invalid.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TField GetStaticValue<TField>() => GenericGetter is Func<TField> func ? func.Invoke() : ThrowStatic<TField>();

        /// <summary>
        /// Sets the strongly typed value of an instance field in a reference type.
        /// If the type of the field or the declaring instance is not known at compile time the non-generic <see cref="Set">Set</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the field.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="instance">The instance that the field belongs to.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant or a static field.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type arguments are invalid.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c>
        /// and this <see cref="FieldAccessor"/> represents a read-only field.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void SetInstanceValue<TInstance, TField>(TInstance instance, TField value) where TInstance : class
        {
            if (GenericSetter is ReferenceTypeAction<TInstance, TField> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), value);
            else
                ThrowInstance<TField>();
        }

        /// <summary>
        /// Gets the strongly typed value of an instance field in a reference type.
        /// If the type of the field or the declaring instance is not known at compile time the non-generic <see cref="Get">Get</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the field.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="instance">The instance that the field belongs to.</param>
        /// <returns>The value of the field.</returns>
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant or a static field.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type arguments are invalid.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TField GetInstanceValue<TInstance, TField>(TInstance instance) where TInstance : class
            => GenericGetter is ReferenceTypeFunction<TInstance, TField> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance))
                : ThrowInstance<TField>();

        /// <summary>
        /// Sets the strongly typed value of an instance field in a value type.
        /// If the type of the field or the declaring instance is not known at compile time the non-generic <see cref="Set">Set</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the field.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="instance">The instance that the field belongs to.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant or a static field.</exception>
        /// <exception cref="ArgumentException">The type arguments are invalid.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c>
        /// and this <see cref="FieldAccessor"/> represents a read-only field.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetInstanceValue<TInstance, TField>(in TInstance instance, TField value) where TInstance : struct
        {
            if (GenericSetter is ValueTypeAction<TInstance, TField> action)
                action.Invoke(instance, value);
            else
                ThrowInstance<TField>();
        }

        /// <summary>
        /// Gets the strongly typed value of an instance field in a value type.
        /// If the type of the field or the declaring instance is not known at compile time the non-generic <see cref="Get">Get</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the field.</typeparam>
        /// <typeparam name="TField">The type of the field.</typeparam>
        /// <param name="instance">The instance that the field belongs to.</param>
        /// <returns>The value of the field.</returns>
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant or a static field.</exception>
        /// <exception cref="ArgumentException">The type arguments are invalid.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TField GetInstanceValue<TInstance, TField>(in TInstance instance) where TInstance : struct
            => GenericGetter is ValueTypeFunction<TInstance, TField> func ? func.Invoke(instance) : ThrowInstance<TField>();

        #endregion

        #region Private Methods

        private Action<object?, object?> CreateSetter()
        {
            Type? declaringType = Field.DeclaringType;
            bool isValueType = declaringType?.IsValueType == true;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!Field.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (IsConstant)
                Throw.InvalidOperationException(Res.ReflectionCannotSetConstantField(MemberInfo.DeclaringType, MemberInfo.Name));
            if (Field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Field.FieldType));

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            // Read-only field or value type: using reflection as fallback
            if (Field.IsInitOnly || isValueType && !Field.IsStatic)
                return Field.SetValue;

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            UnaryExpression castValue = Expression.Convert(valueParameter, Field.FieldType);

            MemberExpression member = Expression.Field(
                Field.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                Field);

            BinaryExpression assign = Expression.Assign(member, castValue);
            Expression<Action<object?, object?>> lambda = Expression.Lambda<Action<object?, object?>>(
                assign,
                instanceParameter, // instance (object)
                valueParameter);
            return lambda.Compile();
#else
            // Expressions would not work for value types and read-only fields so using always dynamic methods
            DynamicMethod dm = new DynamicMethod(setterPrefix + Field.Name, // setter method name
                Reflector.VoidType, // return type
                new[] { Reflector.ObjectType, Reflector.ObjectType }, declaringType ?? Reflector.ObjectType, true); // instance and value parameters

            ILGenerator il = dm.GetILGenerator();

            // if instance field, then processing instance parameter
            if (!Field.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                il.Emit(isValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType!); // casting object instance to target type
            }

            il.Emit(OpCodes.Ldarg_1); // loading 1st argument: value parameter
            il.Emit(Field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, Field.FieldType); // casting object value to field type
            il.Emit(Field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, Field); // processing assignment
            il.Emit(OpCodes.Ret); // returning without return value

            return (Action<object?, object?>)dm.CreateDelegate(typeof(Action<object?, object?>));
#endif
        }

        private Func<object?, object?> CreateGetter()
        {
            Type? declaringType = Field.DeclaringType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (!Field.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Field.FieldType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            MemberExpression member = Expression.Field(
                    // ReSharper disable once AssignNullToNotNullAttribute - the check above prevents null
                    Field.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                    Field);

            var lambda = Expression.Lambda<Func<object?, object?>>(
                    Expression.Convert(member, Reflector.ObjectType), // object return type
                    instanceParameter); // instance (object)
            return lambda.Compile();
        }

        private Delegate CreateGenericSetter()
        {
            Type? declaringType = Field.DeclaringType;
            bool isValueType = declaringType?.IsValueType == true;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (IsConstant)
                Throw.InvalidOperationException(Res.ReflectionCannotSetConstantField(MemberInfo.DeclaringType, MemberInfo.Name));
            if (!Field.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (Field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(Field.FieldType));

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            if (Field.IsInitOnly)
                Throw.PlatformNotSupportedException(Res.ReflectionSetReadOnlyFieldGenericNetStandard20(Field.Name, declaringType));

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
            if (!isValueType)
            {
                instanceParameter = Expression.Parameter(declaringType!, "instance");
                member = Expression.Field(instanceParameter, Field);
                assign = Expression.Assign(member, valueParameter);
                lambda = Expression.Lambda(typeof(ReferenceTypeAction<,>).GetGenericType(declaringType!, Field.FieldType), assign, instanceParameter, valueParameter);
                return lambda.Compile();
            }

            // Struct instance field
            instanceParameter = Expression.Parameter(declaringType!.MakeByRefType(), "instance");
            member = Expression.Field(instanceParameter, Field);
            assign = Expression.Assign(member, valueParameter);
            lambda = Expression.Lambda(typeof(ValueTypeAction<,>).GetGenericType(declaringType, Field.FieldType), assign, instanceParameter, valueParameter);
            return lambda.Compile();
#else
            // Expressions would not work for read-only fields so using always dynamic methods
            Type[] parameterTypes = (Field.IsStatic ? Type.EmptyTypes : new[] { isValueType ? declaringType!.MakeByRefType() : declaringType! })
                .Append(Field.FieldType)
                .ToArray();

            var dm = new DynamicMethod(setterPrefix + Field.Name, Reflector.VoidType, parameterTypes, declaringType ?? Reflector.ObjectType, true);
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
                : isValueType ? typeof(ValueTypeAction<,>).GetGenericType(declaringType!, Field.FieldType)
                : typeof(ReferenceTypeAction<,>).GetGenericType(parameterTypes);
            return dm.CreateDelegate(delegateType);
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

#if NET35
            // Expression.Field fails for .NET Framework 3.5 if the instance is a ByRef type so using DynamicMethod instead
            var dm = new DynamicMethod(getterPrefix + Field.Name, Field.FieldType,
                new[] { declaringType.MakeByRefType() }, declaringType, true);
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // loading 0th argument: instance
            il.Emit(OpCodes.Ldfld, Field); // loading field
            il.Emit(OpCodes.Ret); // returning field value
            return dm.CreateDelegate(typeof(ValueTypeFunction<,>).GetGenericType(declaringType, Field.FieldType));
#else
            // Struct instance field
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            member = Expression.Field(instanceParameter, Field);
            lambda = Expression.Lambda(typeof(ValueTypeFunction<,>).GetGenericType(declaringType, Field.FieldType), member, instanceParameter);
            return lambda.Compile();
#endif
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ContractAnnotation("=> halt"), DoesNotReturn]
        private void PostValidate(object? instance, object? value, Exception exception, bool isSetter)
        {
            if (Field.DeclaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);

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

            ThrowIfSecurityConflict(exception, isSetter ? setterPrefix : getterPrefix);

            // anything else: re-throwing the original exception
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
