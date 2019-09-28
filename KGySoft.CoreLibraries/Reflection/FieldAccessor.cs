#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: FieldAccessor.cs
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
    ///         new PerformanceTest { TestName = "Set Field", Iterations = 1000000 }
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
    /// // Forced CPU Affinity: 2
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
    /// // Forced CPU Affinity: 2
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
        private delegate void FieldSetter(object instance, object value);

        /// <summary>
        /// Represents a non-generic getter that can be used for any fields.
        /// </summary>
        private delegate object FieldGetter(object instance);

        #endregion

        #region Constants

        private const string setterPrefix = "<SetField>__";

        #endregion

        #region Fields

        private FieldGetter getter;
        private FieldSetter setter;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets whether the field is read-only.
        /// </summary>
        /// <remarks>
        /// <note>Even if this property returns <see langword="true"/>&#160;the <see cref="FieldAccessor"/>
        /// is able to set the field.</note>
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
        private FieldGetter Getter => getter ?? (getter = CreateGetter());

        /// <summary>
        /// Gets the field setter delegate.
        /// </summary>
        private FieldSetter Setter => setter ?? (setter = IsConstant 
            ? throw new InvalidOperationException(Res.ReflectionCannotSetConstantField(MemberInfo.DeclaringType, MemberInfo.Name))
            : CreateSetter());

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
        public static FieldAccessor GetAccessor(FieldInfo field)
            => (FieldAccessor)GetCreateAccessor(field ?? throw new ArgumentNullException(nameof(field), Res.ArgumentNull));

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
        /// </remarks>
        public void Set(object instance, object value)
        {
            try
            {
                Setter.Invoke(instance, value);
            }
            catch (VerificationException e) when (IsSecurityConflict(e, setterPrefix))
            {
                throw new NotSupportedException(Res.ReflectionSecuritySettingsConfict, e);
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
        public object Get(object instance) => Getter.Invoke(instance);

        #endregion

        #region Private Methods

        private FieldGetter CreateGetter()
        {
            ParameterExpression instanceParameter = Expression.Parameter(Reflector.ObjectType, "instance");

            FieldInfo field = (FieldInfo)MemberInfo;
            Type declaringType = field.DeclaringType;
            if (!field.IsStatic && declaringType == null)
                throw new InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (field.FieldType.IsPointer)
                throw new NotSupportedException(Res.ReflectionPointerTypeNotSupported(field.FieldType));
            MemberExpression member = Expression.Field(
                    // ReSharper disable once AssignNullToNotNullAttribute - the check above prevents null
                    field.IsStatic ? null : Expression.Convert(instanceParameter, declaringType), // (TInstance)instance
                    field);

            LambdaExpression lambda = Expression.Lambda<FieldGetter>(
                    Expression.Convert(member, Reflector.ObjectType), // object return type
                    instanceParameter); // instance (object)
            return (FieldGetter)lambda.Compile();
        }

        private FieldSetter CreateSetter()
        {
            FieldInfo field = (FieldInfo)MemberInfo;
            Type declaringType = field.DeclaringType;
            if (declaringType == null)
                throw new InvalidOperationException(Res.ReflectionDeclaringTypeExpected);
            if (field.FieldType.IsPointer)
                throw new NotSupportedException(Res.ReflectionPointerTypeNotSupported(field.FieldType));

#if NETSTANDARD2_0
            // DynamicMethod is not available in .NET Standard 2.0 so using expressions, which cannot be used for instance struct fields
            //if (!declaringType.IsValueType || field.IsStatic)
            {
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
            }

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
                // casting object instance to target type
                if (declaringType.IsValueType)
                {
                    // Note: this is a tricky solution that cannot be made in C#:
                    // We are just unboxing the value type without storing it in a typed local variable
                    // This makes possible to preserve the modified content of a value type without using ref parameter
                    il.Emit(OpCodes.Unbox, declaringType); // unboxing the instance

                    // If instance parameter was a ref parameter, then it should be unboxed into a local variable:
                    //LocalBuilder typedInstance = il.DeclareLocal(declaringType);
                    //il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                    //il.Emit(OpCodes.Ldind_Ref); // as a reference - in dm instance parameter must be defined as: Reflector.ObjectType.MakeByRefType()
                    //il.Emit(OpCodes.Unbox_Any, declaringType); // unboxing the instance
                    //il.Emit(OpCodes.Stloc_0); // saving value into 0. local
                    //il.Emit(OpCodes.Ldloca_S, typedInstance);
                }
                else
                {
                    il.Emit(OpCodes.Castclass, declaringType);

                    // If instance parameter was a ref parameter, then it should be unboxed into a local variable:
                    //LocalBuilder typedInstance = il.DeclareLocal(declaringType);
                    //il.Emit(OpCodes.Ldarg_0); // loading 0th argument (instance)
                    //il.Emit(OpCodes.Ldind_Ref); // as a reference - in dm instance parameter must be defined as: Reflector.ObjectType.MakeByRefType()
                    //il.Emit(OpCodes.Castclass, declaringType); // casting the instance
                    //il.Emit(OpCodes.Stloc_0); // saving value into 0. local
                    //il.Emit(OpCodes.Ldloca_S, typedInstance);
                }
            }

            // processing 1st argument: value parameter
            il.Emit(OpCodes.Ldarg_1);

            // casting object value to field type
            il.Emit(field.FieldType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, field.FieldType);

            // processing assignment
            il.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);

            // returning without return value
            il.Emit(OpCodes.Ret);

            return (FieldSetter)dm.CreateDelegate(typeof(FieldSetter));
#endif
        }

        #endregion

        #endregion

        #endregion
    }
}
