#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FieldAccessor.cs
///////////////////////////////////////////////////////////////////////////////
//  Copyright (C) KGy SOFT, 2005-2021 - All Rights Reserved
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
using System.Runtime.CompilerServices;
using System.Security;

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
        #region Delegates

        /// <summary>
        /// Represents a non-generic setter that can be used for any fields.
        /// </summary>
        private delegate void FieldSetter(object? instance, object? value);

        /// <summary>
        /// Represents a non-generic getter that can be used for any fields.
        /// </summary>
        private delegate object? FieldGetter(object? instance);

        #endregion

        #region Constants

        private const string setterPrefix = "<SetField>__";

        #endregion

        #region Fields

        private FieldGetter? getter;
        private FieldSetter? setter;

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

        /// <summary>
        /// Gets the field getter delegate.
        /// </summary>
        private FieldGetter Getter
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get => getter ??= CreateGetter();
        }

        /// <summary>
        /// Gets the field setter delegate.
        /// </summary>
        private FieldSetter Setter
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if (setter != null)
                    return setter;
                if (IsConstant)
                    Throw.InvalidOperationException(Res.ReflectionCannotSetConstantField(MemberInfo.DeclaringType, MemberInfo.Name));
                return setter = CreateSetter();
            }
        }

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
        /// <param name="instance">The instance that the field belongs to. Can be <see langword="null"/>&#160;for static fields.</param>
        /// <param name="value">The value to be set.</param>
        /// <remarks>
        /// <note>
        /// Setting the field for the first time is slower than the <see cref="FieldInfo.SetValue(object,object)">System.Reflection.FieldInfo.SetValue</see>
        /// method but further calls are much faster.
        /// </note>
        /// <note type="caller">Calling the .NET Standard 2.0 version of this method throws a <see cref="PlatformNotSupportedException"/>
        /// if the field to set is read-only or is an instance member of a value type (<see langword="struct"/>).
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.SetField">Reflector.SetField</see> methods to set read-only or value type instance fields.</note>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void Set(object? instance, object? value)
        {
            try
            {
                Setter.Invoke(instance, value);
            }
            catch (VerificationException e) when (IsSecurityConflict(e, setterPrefix))
            {
                Throw.NotSupportedException(Res.ReflectionSecuritySettingsConflict, e);
            }
        }

        /// <summary>
        /// Gets the value of the field.
        /// For static fields the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// </summary>
        /// <param name="instance">The instance that the field belongs to. Can be <see langword="null"/>&#160;for static fields.</param>
        /// <returns>The value of the field.</returns>
        /// <remarks>
        /// <note>
        /// Getting the field for the first time is slower than the <see cref="FieldInfo.GetValue">System.Reflection.FieldInfo.GetValue</see>
        /// method but further calls are much faster.
        /// </note>
        /// </remarks>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public object? Get(object? instance) => Getter.Invoke(instance);

        #endregion

        #region Private Methods

        private FieldGetter CreateGetter()
        {
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");

            FieldInfo field = (FieldInfo)MemberInfo;
            Type? declaringType = field.DeclaringType;
            if (!field.IsStatic && declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(field.FieldType));
            MemberExpression member = Expression.Field(
                    // ReSharper disable once AssignNullToNotNullAttribute - the check above prevents null
                    field.IsStatic ? null : Expression.Convert(instanceParameter, declaringType!), // (TInstance)instance
                    field);

            LambdaExpression lambda = Expression.Lambda<FieldGetter>(
                    Expression.Convert(member, Reflector.ObjectType), // object return type
                    instanceParameter); // instance (object)
            return (FieldGetter)lambda.Compile();
        }

        private FieldSetter CreateSetter()
        {
            FieldInfo field = (FieldInfo)MemberInfo;
            Type? declaringType = field.DeclaringType;
            if (declaringType == null)
                Throw.InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (field.FieldType.IsPointer)
                Throw.NotSupportedException(Res.ReflectionPointerTypeNotSupported(field.FieldType));

#if NETSTANDARD2_0 // DynamicMethod and ILGenerator is not available in .NET Standard 2.0
            if (field.IsInitOnly)
                Throw.PlatformNotSupportedException(Res.ReflectionSetReadOnlyFieldNetStandard20(field.Name, declaringType));
            if (!field.IsStatic && declaringType.IsValueType)
                Throw.PlatformNotSupportedException(Res.ReflectionSetStructFieldNetStandard20(field.Name, declaringType));

            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");
            ParameterExpression valueParameter = Expression.Parameter(Reflector.ObjectType, "value");
            UnaryExpression castValue = Expression.Convert(valueParameter, field.FieldType);

            MemberExpression member = Expression.Field(
                field.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                field);

            BinaryExpression assign = Expression.Assign(member, castValue);
            Expression<FieldSetter> lambda = Expression.Lambda<FieldSetter>(
                assign,
                instanceParameter, // instance (object)
                valueParameter);
            return lambda.Compile();
#else
            // Expressions would not work for value types so using always dynamic methods
            DynamicMethod dm = new DynamicMethod(setterPrefix + field.Name, // setter method name
                Reflector.VoidType, // return type
                new[] { Reflector.ObjectType, Reflector.ObjectType }, declaringType, true); // instance and value parameters

            ILGenerator il = dm.GetILGenerator();

            // if instance field, then processing instance parameter
            if (!field.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                il.Emit(declaringType.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, declaringType); // casting object instance to target type
            }

            il.Emit(OpCodes.Ldarg_1); // loading 1st argument: value parameter
            il.Emit(field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, field.FieldType); // casting object value to field type
            il.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field); // processing assignment
            il.Emit(OpCodes.Ret); // returning without return value

            return (FieldSetter)dm.CreateDelegate(typeof(FieldSetter));
#endif
        }

        #endregion

        #endregion

        #endregion
    }
}
