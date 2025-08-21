#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FieldAccessor.cs
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
using System.Diagnostics.CodeAnalysis;
#if !NETSTANDARD2_0
using System.Linq;
#endif
#if NETSTANDARD2_0
using System.Linq.Expressions;
#endif
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
    /// <div style="display: none;"><br/>See the <a href="https://docs.kgysoft.net/corelibraries/html/T_KGySoft_Reflection_FieldAccessor.htm">online help</a> for an example.</div>
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="FieldAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.</para>
    /// <para>The non-generic <see cref="Get">Get</see> and <see cref="Set">Set</see> methods can be used to get and set the field in general cases.</para>
    /// <para>If you know the field type at compile time, then you can use the generic <see cref="GetStaticValue{TField}">GetStaticValue</see>/<see cref="SetStaticValue{TField}">SetStaticValue</see>
    /// methods for static fields. If you know also the instance type, then
    /// the <see cref="O:KGySoft.Reflection.FieldAccessor.GetInstanceValue">GetInstanceValue</see>/<see cref="O:KGySoft.Reflection.FieldAccessor.SetInstanceValue">SetInstanceValue</see>
    /// methods can be used to access instance fields with better performance.</para>
    /// <para>The first call of these methods are slower because the delegates are generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to access a field by name rather than by a <see cref="FieldInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.SetField">SetField</see>
    /// and <see cref="O:KGySoft.Reflection.Reflector.SetField">GetField</see> methods in the <see cref="Reflector"/> class, which have some overloads with a <c>fieldName</c> parameter.</note>
    /// <note type="caution">The generic setter methods of this class in the .NET Standard 2.0 build throw a <see cref="PlatformNotSupportedException"/>
    /// for read-only and pointer instance fields of value types. Use the non-generic <see cref="Set">Set</see> method or reference the .NET Standard 2.1 build or any .NET Framework or .NET Core/.NET builds
    /// to setting support setting read-only fields by the generic setters.</note>
    /// </remarks>
    /// <example>
    /// The following example compares the <see cref="FieldAccessor"/> class with <see cref="FieldInfo"/> on .NET 8 and .NET Framework 4.8 platforms.
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Reflection;
    /// using System.Runtime.Versioning;
    /// 
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
    ///     private static string PlatformName => ((TargetFrameworkAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(),
    ///         typeof(TargetFrameworkAttribute))).FrameworkDisplayName;
    /// 
    ///     static void Main(string[] args)
    ///     {
    ///         var instance = new TestClass();
    ///         FieldInfo field = instance.GetType().GetField(nameof(TestClass.TestField));
    ///         FieldAccessor accessor = FieldAccessor.GetAccessor(field);
    /// 
    ///         new PerformanceTest { TestName = $"Set Field - {PlatformName}", Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestField = 1, "Direct set")
    ///             .AddCase(() => field.SetValue(instance, 1), "System.Reflection.FieldInfo.SetValue")
    ///             .AddCase(() => accessor.Set(instance, 1), "FieldAccessor.Set")
    ///             .AddCase(() => accessor.SetInstanceValue(instance, 1), "FieldAccessor.SetInstanceValue<,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    /// 
    ///         new PerformanceTest<int> { TestName = $"Get Field - {PlatformName}", Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestField, "Direct get")
    ///             .AddCase(() => (int)field.GetValue(instance), "System.Reflection.FieldInfo.GetValue")
    ///             .AddCase(() => (int)accessor.Get(instance), "FieldAccessor.Get")
    ///             .AddCase(() => accessor.GetInstanceValue<TestClass, int>(instance), "FieldAccessor.GetInstanceValue<,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to these ones:
    /// 
    /// // ==[Set Field - .NET 8.0 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct set: average time: 2.83 ms
    /// // 2. FieldAccessor.SetInstanceValue<,>: average time: 4.53 ms (+1.71 ms / 160.31%)
    /// // 3. FieldAccessor.Set: average time: 10.84 ms (+8.01 ms / 383.41%)
    /// // 4. System.Reflection.FieldInfo.SetValue: average time: 52.15 ms (+49.33 ms / 1,844.55%)
    /// // 
    /// // ==[Get Field - .NET 8.0 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct get: average time: 2.83 ms
    /// // 2. FieldAccessor.GetInstanceValue<,>: average time: 3.75 ms (+0.92 ms / 132.42%)
    /// // 3. FieldAccessor.Get: average time: 10.18 ms (+7.35 ms / 359.81%)
    /// // 4. System.Reflection.FieldInfo.GetValue: average time: 54.13 ms (+51.30 ms / 1,913.66%)
    /// 
    /// // ==[Set Field - .NET Framework 4.8 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct set: average time: 3.11 ms
    /// // 2. FieldAccessor.SetInstanceValue<,>: average time: 11.45 ms (+8.34 ms / 367.99%)
    /// // 3. FieldAccessor.Set: average time: 13.65 ms (+10.54 ms / 438.45%)
    /// // 4. System.Reflection.FieldInfo.SetValue: average time: 99.95 ms (+96.84 ms / 3,211.03%)
    /// // 
    /// // ==[Get Field - .NET Framework 4.8 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct get: average time: 2.15 ms
    /// // 2. FieldAccessor.GetInstanceValue<,>: average time: 10.24 ms (+8.09 ms / 476.41%)
    /// // 3. FieldAccessor.Get: average time: 10.56 ms (+8.41 ms / 491.29%)
    /// // 4. System.Reflection.FieldInfo.GetValue: average time: 75.45 ms (+73.30 ms / 3,509.27%)]]></code>
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
        /// <note>Even if this property returns <see langword="true"/> the <see cref="FieldAccessor"/> is able to set the field,
        /// except if the .NET Standard 2.0 build of the <c>KGySoft.CoreLibraries</c> assembly is used and the field is an instance field of a value type,
        /// in which case doing so throws a <see cref="PlatformNotSupportedException"/>.</note>
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
        /// <note type="caller">If the field is read-only, has a pointer type or is an instance field of a value type, then the .NET Standard 2.0 version of this method
        /// defaults to use regular reflection to preserve mutations. To experience the best performance try to target .NET Standard 2.1
        /// or any .NET Framework or .NET Core/.NET platforms instead.</note>
        /// </remarks>
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant field or a field of an open generic type.</exception>
        /// <exception cref="ArgumentNullException">This <see cref="FieldAccessor"/> represents an instance field and <paramref name="instance"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/>This <see cref="FieldAccessor"/> represents a value type field and <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> or <paramref name="value"/> is invalid.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
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
                // Post-validation if there was any exception. We do this for better performance on the happy path.
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
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a field of an open generic type.</exception>
        /// <exception cref="ArgumentNullException">This <see cref="FieldAccessor"/> represents an instance field and <paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> is invalid.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
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
                // Post-validation if there was any exception. We do this for better performance on the happy path.
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
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant, an instance field or a field of an open generic type.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="TField"/> is invalid.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
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
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents an instance field or a field of an open generic type.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="TField"/> is invalid.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
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
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant, a static field or a field of an open generic type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c>
        /// and this <see cref="FieldAccessor"/> represents a read-only or a pointer field.</exception>
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
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a static field or a field of an open generic type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
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
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a constant, a static field or a field of an open generic type.</exception>
        /// <exception cref="ArgumentException">The type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c>
        /// and this <see cref="FieldAccessor"/> represents a read-only or a pointer field.</exception>
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
        /// <exception cref="InvalidOperationException">This <see cref="FieldAccessor"/> represents a static field or a field of an open generic type.</exception>
        /// <exception cref="ArgumentException">The type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
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

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            // Read-only/pointer field or value type: using reflection as fallback
            if (Field.IsInitOnly || isValueType && !Field.IsStatic || Field.FieldType.IsPointer)
            {
                // pointer field: explicitly casting to IntPtr; otherwise, even a string could be passed without an error
                return Field.FieldType.IsPointer
                    ? (instance, value) => Field.SetValue(instance, (IntPtr)value!)
                    : Field.SetValue;
            }

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
            il.Emit(Field.FieldType.IsValueType || Field.FieldType.IsPointer ? OpCodes.Unbox_Any : OpCodes.Castclass, Field.FieldType.IsPointer ? typeof(IntPtr) : Field.FieldType); // casting object value to field type
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

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            // Pointer field: Fallback to reflection
            if (Field.FieldType.IsPointer)
                unsafe { return instance => (IntPtr)Pointer.Unbox(Field.GetValue(instance)); }

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            MemberExpression member = Expression.Field(
                    // ReSharper disable once AssignNullToNotNullAttribute - the check above prevents null
                    Field.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                    Field);

            var lambda = Expression.Lambda<Func<object?, object?>>(
                    Expression.Convert(member, Reflector.ObjectType), // object return type
                    instanceParameter); // instance (object)
            return lambda.Compile();
#else
            DynamicMethod dm = new DynamicMethod(getterPrefix + Field.Name, // getter method name
                Reflector.ObjectType, // return type
                new[] { Reflector.ObjectType }, declaringType ?? Reflector.ObjectType, true); // instance parameter
            ILGenerator il = dm.GetILGenerator();
            bool isValueType = declaringType?.IsValueType == true;

            if (Field.IsStatic)
                il.Emit(OpCodes.Ldsfld, Field); // loading static field
            else
            {
                il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                il.Emit(isValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType!); // casting object instance to target type
                il.Emit(OpCodes.Ldfld, Field); // loading instance field
            }

            if (Field.FieldType.IsValueType || Field.FieldType.IsPointer)
                il.Emit(OpCodes.Box, Field.FieldType.IsPointer ? typeof(IntPtr) : Field.FieldType); // boxing value type or pointer result
            il.Emit(OpCodes.Ret); // returning with the field value
            return (Func<object?, object?>)dm.CreateDelegate(typeof(Func<object?, object?>));
#endif
        }

        private Delegate CreateGenericSetter()
        {
            Type? declaringType = Field.DeclaringType;
            bool isValueType = declaringType?.IsValueType == true;
            bool isStatic = Field.IsStatic;
            Type fieldValueType = Field.FieldType.IsPointer ? typeof(IntPtr) : Field.FieldType;
            if (declaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);
            if (IsConstant)
                Throw.InvalidOperationException(Res.ReflectionCannotSetConstantField(MemberInfo.DeclaringType, MemberInfo.Name));
            if (!isStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

            Type delegateType = isStatic ? typeof(Action<>).GetGenericType(fieldValueType)
                : isValueType ? typeof(ValueTypeAction<,>).GetGenericType(declaringType!, fieldValueType)
                : typeof(ReferenceTypeAction<,>).GetGenericType(declaringType!, fieldValueType);

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            LambdaExpression lambda;

            // Read-only field or value type: using reflection as fallback
            if (Field.IsInitOnly || Field.FieldType.IsPointer)
            {
                // value types: the ref instance parameter would be boxed, losing the mutability
                if (isValueType && !isStatic)
                {
                    if (Field.IsInitOnly)
                        Throw.PlatformNotSupportedException(Res.ReflectionSetReadOnlyFieldGenericNetStandard20(Field.Name, declaringType));
                    if (Field.FieldType.IsPointer)
                        Throw.PlatformNotSupportedException(Res.ReflectionValueTypeWithPointersGenericNetStandard20);
                }

                ParameterExpression[] parameters = new ParameterExpression[isStatic ? 1 : 2];
                int valueIndex = isStatic ? 0 : 1;
                if (!isStatic)
                    parameters[0] = Expression.Parameter(declaringType!, "instance");
                parameters[valueIndex] = Expression.Parameter(fieldValueType, "value");

                Expression[] methodParameters = new Expression[2];
                methodParameters[0] = isStatic
                    ? Expression.Constant(null, typeof(object))
                    : Expression.Convert(parameters[0], typeof(object));
                methodParameters[1] = Expression.Convert(parameters[valueIndex], typeof(object));

                MethodCallExpression methodCall = Expression.Call(
                    Expression.Constant(Field), // the instance is the FieldInfo itself
                    Field.GetType().GetMethod(nameof(FieldInfo.SetValue), [typeof(object), typeof(object)])!, // SetValue(object, object)
                    methodParameters);

                lambda = Expression.Lambda(delegateType, methodCall, parameters);
                return lambda.Compile();
            }

            ParameterExpression instanceParameter;
            ParameterExpression valueParameter = Expression.Parameter(Field.FieldType, "value");
            MemberExpression member;
            BinaryExpression assign;

            // Static field
            if (isStatic)
            {
                member = Expression.Field(null, Field);
                assign = Expression.Assign(member, valueParameter);
                lambda = Expression.Lambda(delegateType, assign, valueParameter);
                return lambda.Compile();
            }

            // Class instance field
            if (!isValueType)
            {
                instanceParameter = Expression.Parameter(declaringType!, "instance");
                member = Expression.Field(instanceParameter, Field);
                assign = Expression.Assign(member, valueParameter);
                lambda = Expression.Lambda(delegateType, assign, instanceParameter, valueParameter);
                return lambda.Compile();
            }

            // Struct instance field
            instanceParameter = Expression.Parameter(declaringType!.MakeByRefType(), "instance");
            member = Expression.Field(instanceParameter, Field);
            assign = Expression.Assign(member, valueParameter);
            lambda = Expression.Lambda(delegateType, assign, instanceParameter, valueParameter);
            return lambda.Compile();
#else
            Type[] parameterTypes = (isStatic ? Type.EmptyTypes : [isValueType ? declaringType!.MakeByRefType() : declaringType!])
                .Append(fieldValueType)
                .ToArray();

            var dm = new DynamicMethod(setterPrefix + Field.Name, Reflector.VoidType, parameterTypes, declaringType ?? Reflector.ObjectType, true);
            ILGenerator il = dm.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0); // loading 0th argument: instance for instance fields, value for static fields
            if (isStatic)
                il.Emit(OpCodes.Stsfld, Field); // assigning static field
            else
            {
                il.Emit(OpCodes.Ldarg_1); // loading 1st argument: value parameter for instance fields
                il.Emit(OpCodes.Stfld, Field); // assigning instance field
            }

            il.Emit(OpCodes.Ret); // returning without return value
            return dm.CreateDelegate(delegateType);
#endif
        }

        private Delegate CreateGenericGetter()
        {
            Type? declaringType = Field.DeclaringType;
            bool isValueType = declaringType?.IsValueType == true;
            bool isStatic = Field.IsStatic;
            if (!isStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);

            Type returnType = Field.FieldType.IsPointer ? typeof(IntPtr) : Field.FieldType;
            Type delegateType = isStatic ? typeof(Func<>).GetGenericType(returnType)
                : isValueType ? typeof(ValueTypeFunction<,>).GetGenericType(declaringType!, returnType)
                : typeof(ReferenceTypeFunction<,>).GetGenericType(declaringType!, returnType);

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            MemberExpression member;
            ParameterExpression instanceParameter;
            LambdaExpression lambda;

            // Pointer property: fallback to Getter.Invoke(object), which supports pointers as IntPtr.
            // NOTE: Unlike in the setter, we cannot use FieldInfo.GetValue(object) here, because we should call Pointer.Unbox(object) on the result,
            // which is not possible by Expression trees.
            if (Field.FieldType.IsPointer)
            {
                ParameterExpression[] parameters = new ParameterExpression[isStatic ? 0 : 1];
                if (!isStatic)
                    parameters[0] = Expression.Parameter(isValueType ? declaringType!.MakeByRefType() : declaringType!, "instance");

                Expression[] methodParameters = new Expression[1];
                methodParameters[0] = isStatic
                    ? Expression.Constant(null, typeof(object))
                    : Expression.Convert(parameters[0], typeof(object));

                MethodCallExpression methodCall = Expression.Call(
                    Expression.Constant(Getter),
                    Getter.GetType().GetMethod("Invoke", [typeof(object)])!,
                    methodParameters);

                lambda = Expression.Lambda(delegateType, Expression.Convert(methodCall, returnType), parameters);
                return lambda.Compile();
            }

            // Static field
            if (isStatic)
            {
                member = Expression.Field(null, Field);
                lambda = Expression.Lambda(delegateType, member);
                return lambda.Compile();
            }

            // Class instance field
            if (!declaringType!.IsValueType)
            {
                instanceParameter = Expression.Parameter(declaringType, "instance");
                member = Expression.Field(instanceParameter, Field);
                lambda = Expression.Lambda(delegateType, member, instanceParameter);
                return lambda.Compile();
            }

            // Struct instance field
            instanceParameter = Expression.Parameter(declaringType.MakeByRefType(), "instance");
            member = Expression.Field(instanceParameter, Field);
            lambda = Expression.Lambda(delegateType, member, instanceParameter);
            return lambda.Compile();

#else
            Type fieldValueType = Field.FieldType.IsPointer ? typeof(IntPtr) : Field.FieldType;
            Type[] parameterTypes = isStatic ? Type.EmptyTypes
                : isValueType ? [declaringType!.MakeByRefType()]
                : [declaringType!];
            var dm = new DynamicMethod(getterPrefix + Field.Name, fieldValueType,
                parameterTypes,
                declaringType ?? Reflector.ObjectType, true);
            ILGenerator il = dm.GetILGenerator();
            if (isStatic)
            {
                il.Emit(OpCodes.Ldsfld, Field); // loading static field
                il.Emit(OpCodes.Ret); // returning field value
                return dm.CreateDelegate(delegateType);
            }

            il.Emit(OpCodes.Ldarg_0); // loading 0th argument: instance
            il.Emit(OpCodes.Ldfld, Field); // loading field
            il.Emit(OpCodes.Ret); // returning field value
            return dm.CreateDelegate(delegateType);
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
                    Type valueParamType = Field.FieldType.IsPointer ? typeof(IntPtr) : Field.FieldType;
                    if (value == null)
                        Throw.ArgumentNullException(Argument.value, Res.NotAnInstanceOfType(valueParamType));
                    Throw.ArgumentException(Argument.value, Res.NotAnInstanceOfType(valueParamType));
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
