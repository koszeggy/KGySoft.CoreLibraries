#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PropertyAccessor.cs
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
using System.Linq;
using System.Reflection;
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
    /// Provides an efficient way for setting and getting property values via dynamically created delegates.
    /// <div style="display: none;"><br/>See the <a href="https://docs.kgysoft.net/corelibraries/html/T_KGySoft_Reflection_PropertyAccessor.htm">online help</a> for an example.</div>
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="PropertyAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.</para>
    /// <para>The <see cref="Get(object, object[])"/> and <see cref="Set(object, object, object[])"/> methods can be used to get or set the property in general cases.
    /// These methods can be used for any properties, including indexed ones.</para>
    /// <para>The other non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Get">Get</see> and <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see>
    /// overloads can be used for simple properties or for indexers with one index parameter.</para>
    /// <para>If you know the property type at compile time, then you can use the generic <see cref="GetStaticValue{TProperty}">GetStaticValue</see>/<see cref="SetStaticValue{TProperty}">SetStaticValue</see>
    /// methods for static properties. If you know also the instance type (and the index parameter for indexers), then
    /// the <see cref="O:KGySoft.Reflection.PropertyAccessor.GetInstanceValue">GetInstanceValue</see>/<see cref="O:KGySoft.Reflection.PropertyAccessor.SetInstanceValue">SetInstanceValue</see>
    /// methods can be used to access instance properties with better performance. These generic methods can be used for properties with no more than one index parameter.</para>
    /// <para>The first call of these methods are slower because the delegates are generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to access a property by name rather than by a <see cref="PropertyInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.SetProperty">SetProperty</see>
    /// and <see cref="O:KGySoft.Reflection.Reflector.SetProperty">GetProperty</see> methods in the <see cref="Reflector"/> class, which have some overloads with a <c>propertyName</c> parameter.</note>
    /// <note type="caution">The getter/setter methods of this class in the .NET Standard 2.0 version throw a <see cref="PlatformNotSupportedException"/>
    /// for <see langword="ref"/> properties. You need to reference the .NET Standard 2.1 build or any .NET Framework or .NET Core/.NET builds to support <see langword="ref"/> properties.</note>
    /// </remarks>
    /// <example>
    /// The following example compares the <see cref="PropertyAccessor"/> class with <see cref="PropertyInfo"/> on .NET 8 and .NET Framework 4.8 platforms.
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
    ///         public int TestProperty { get; set; }
    ///     }
    /// 
    ///     private static string PlatformName => ((TargetFrameworkAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(),
    ///         typeof(TargetFrameworkAttribute))).FrameworkDisplayName;
    /// 
    ///     static void Main(string[] args)
    ///     {
    ///         var instance = new TestClass();
    ///         PropertyInfo property = instance.GetType().GetProperty(nameof(TestClass.TestProperty));
    ///         PropertyAccessor accessor = PropertyAccessor.GetAccessor(property);
    /// 
    ///         new PerformanceTest { TestName = $"Set Property - {PlatformName}", Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestProperty = 1, "Direct set")
    ///             .AddCase(() => property.SetValue(instance, 1), "System.Reflection.PropertyInfo.SetValue")
    ///             .AddCase(() => accessor.Set(instance, 1), "PropertyAccessor.Set")
    ///             .AddCase(() => accessor.SetInstanceValue(instance, 1), "PropertyAccessor.SetInstanceValue<,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    /// 
    ///         new PerformanceTest<int> { TestName = $"Get Property - {PlatformName}", Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestProperty, "Direct get")
    ///             .AddCase(() => (int)property.GetValue(instance), "System.Reflection.PropertyInfo.GetValue")
    ///             .AddCase(() => (int)accessor.Get(instance), "PropertyAccessor.Get")
    ///             .AddCase(() => accessor.GetInstanceValue<TestClass, int>(instance), "PropertyAccessor.GetInstanceValue<,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to these ones:
    /// 
    /// // ==[Set Property - .NET 8.0 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct set: average time: 3.02 ms
    /// // 2. PropertyAccessor.SetInstanceValue<,>: average time: 4.70 ms (+1.67 ms / 155.43%)
    /// // 3. PropertyAccessor.Set: average time: 11.60 ms (+8.58 ms / 384.08%)
    /// // 4. System.Reflection.PropertyInfo.SetValue: average time: 25.79 ms (+22.77 ms / 853.68%)
    /// // 
    /// // ==[Get Property - .NET 8.0 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct get: average time: 2.86 ms
    /// // 2. PropertyAccessor.GetInstanceValue<,>: average time: 4.80 ms (+1.95 ms / 168.15%)
    /// // 3. PropertyAccessor.Get: average time: 11.56 ms (+8.71 ms / 404.98%)
    /// // 4. System.Reflection.PropertyInfo.GetValue: average time: 17.05 ms (+14.20 ms / 597.12%)
    /// 
    /// // ==[Set Property - .NET Framework 4.8 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct set: average time: 3.63 ms
    /// // 2. PropertyAccessor.SetInstanceValue<,>: average time: 12.05 ms (+8.42 ms / 331.91%)
    /// // 3. PropertyAccessor.Set: average time: 18.20 ms (+14.57 ms / 501.41%)
    /// // 4. System.Reflection.PropertyInfo.SetValue: average time: 186.50 ms (+182.87 ms / 5,137.78%)
    /// // 
    /// // ==[Get Property - .NET Framework 4.8 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct get: average time: 3.51 ms
    /// // 2. PropertyAccessor.GetInstanceValue<,>: average time: 11.12 ms (+7.62 ms / 317.21%)
    /// // 3. PropertyAccessor.Get: average time: 15.01 ms (+11.50 ms / 428.04%)
    /// // 4. System.Reflection.PropertyInfo.GetValue: average time: 120.99 ms (+117.49 ms / 3,450.32%)]]></code>
    /// </example>
    public abstract class PropertyAccessor : MemberAccessor
    {
        #region Fields

        private Action<object?, object?, object?[]?>? generalSetter;
        private Func<object?, object?[]?, object?>? generalGetter;
        private Delegate? genericSetter;
        private Delegate? genericGetter;
        private Delegate? nonGenericSetter;
        private Delegate? nonGenericGetter;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets whether the property can be read (has get accessor).
        /// </summary>
        public bool CanRead => Property.CanRead;

        /// <summary>
        /// Gets whether the property can be written (has set accessor or is a <see langword="ref"/> property).
        /// </summary>
        public bool CanWrite => Property.CanWrite || Property.PropertyType.IsByRef;

        #endregion

        #region Private Protected Properties

        private protected PropertyInfo Property => (PropertyInfo)MemberInfo;
        private protected Action<object?, object?, object?[]?> GeneralSetter => generalSetter ??= CreateGeneralSetter();
        private protected Func<object?, object?[]?, object?> GeneralGetter => generalGetter ??= CreateGeneralGetter();
        private protected Delegate GenericSetter => genericSetter ??= CreateGenericSetter();
        private protected Delegate GenericGetter => genericGetter ??= CreateGenericGetter();
        private protected Delegate NonGenericSetter => nonGenericSetter ??= CreateNonGenericSetter();
        private protected Delegate NonGenericGetter => nonGenericGetter ??= CreateNonGenericGetter();

        #endregion

        #region Private Properties

        private bool IsStatic => (Property.GetGetMethod(true) ?? Property.GetSetMethod(true)!).IsStatic;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyAccessor"/> class.
        /// </summary>
        /// <param name="property">The property for which the accessor is to be created.</param>
        private protected PropertyAccessor(PropertyInfo property) :
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract - null check is in base so it is needed here
            base(property, property?.GetIndexParameters().Select(p => p.ParameterType).ToArray())
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="PropertyAccessor"/> for the specified <paramref name="property"/>.
        /// </summary>
        /// <param name="property">The property for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="PropertyAccessor"/> instance that can be used to get or set the property.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static PropertyAccessor GetAccessor(PropertyInfo property)
        {
            if (property == null!)
                Throw.ArgumentNullException(Argument.property);
            return (PropertyAccessor)GetCreateAccessor(property);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates an accessor for a property without caching.
        /// </summary>
        /// <param name="property">The property for which an accessor should be created.</param>
        /// <returns>A <see cref="PropertyAccessor"/> instance that can be used to get or set the property.</returns>
        internal static PropertyAccessor CreateAccessor(PropertyInfo property)
            => property.GetIndexParameters().Length == 0
                ? new SimplePropertyAccessor(property)
                : new IndexerAccessor(property);

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Sets the property.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// If the property is not an indexer, then <paramref name="indexParameters"/> parameter is omitted.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/> for static properties.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="indexParameters">The parameters if the property is an indexer.</param>
        /// <remarks>
        /// <para>Setting the property for the first time is slower than the <see cref="PropertyInfo.SetValue(object,object,object[])">System.Reflection.PropertyInfo.SetValue</see>
        /// method but further calls are much faster.</para>
        /// <para>The method can be use also for <see langword="ref"/> properties.</para>
        /// <note type="tip">If the property has no more than one index parameters and you know the type of the property at compile time
        /// (and also the declaring type for instance properties), then you can use the generic <see cref="SetStaticValue{TProperty}">SetStaticValue</see>
        /// or <see cref="O:KGySoft.Reflection.PropertyAccessor.SetInstanceValue">SetInstanceValue</see> methods for better performance.</note>
        /// <note type="caller">If the property is an instance property of a value type or has a pointer type or pointer index parameter,
        /// then the .NET Standard 2.0 version of this method defaults to use regular reflection
        /// to preserve mutations. To experience the best performance try to target .NET Standard 2.1 or any .NET Framework or .NET Core/.NET platforms instead.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">This <see cref="PropertyAccessor"/> represents an instance property and <paramref name="instance"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/>This <see cref="PropertyAccessor"/> represents a value type property and <paramref name="value"/> is <see langword="null"/>.
        /// <br/>-or-
        /// <br/>This <see cref="PropertyAccessor"/> represents an indexed property and <paramref name="indexParameters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/>, <paramref name="value"/> or one of the <paramref name="indexParameters"/> is invalid.
        /// <br/>-or-
        /// <br/><paramref name="indexParameters"/> has too few elements.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="PropertyAccessor"/> represents a property of an open generic type.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a read-only property.
        /// <br/>-or-
        /// <br/>On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        /// <overloads>The <see cref="Set(object, object, object[])"/> overload can be used for any number of index parameters.
        /// The other non-generic overloads can be used for simple properties or indexers with one parameter.
        /// <note type="tip">If you know the property type at compile time, then you can use the generic <see cref="SetStaticValue{TProperty}">SetStaticValue</see>
        /// overloads for static properties. If you know also the instance type (and the index parameter for indexers), then
        /// the <see cref="O:KGySoft.Reflection.PropertyAccessor.SetInstanceValue">SetInstanceValue</see>
        /// methods can be used to access instance properties with better performance.</note></overloads>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public void Set(object? instance, object? value, params object?[]? indexParameters)
        {
            try
            {
                GeneralSetter.Invoke(instance, value, indexParameters);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(instance, value, indexParameters, e, true, true);
            }
        }

        /// <summary>
        /// Sets the property with no index parameters.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Set(object,object,object[])"/> overload for details.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/> for static properties.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="ArgumentNullException">This <see cref="PropertyAccessor"/> represents an instance property and <paramref name="instance"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/>This <see cref="PropertyAccessor"/> represents a value type property and <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> or <paramref name="value"/> is invalid.
        /// <br/>-or-
        /// <br/>The property expects index parameters.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="PropertyAccessor"/> represents a property of an open generic type.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a read-only property.
        /// <br/>-or-
        /// <br/>On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public void Set(object? instance, object? value)
        {
            try
            {
                ((Action<object?, object?>)NonGenericSetter).Invoke(instance, value);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(instance, value, Reflector.EmptyObjects, e, true, false);
            }
        }

        /// <summary>
        /// Sets the property with one index parameter.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Set(object,object,object[])"/> overload for details.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/> for static properties.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="index">The value of the index.</param>
        /// <exception cref="ArgumentNullException">This <see cref="PropertyAccessor"/> represents an instance property and <paramref name="instance"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/>This <see cref="PropertyAccessor"/> represents a value type property and <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/>, <paramref name="value"/> or <paramref name="index"/> is invalid.
        /// <br/>-or-
        /// <br/>The property cannot be accessed with one index parameter.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="PropertyAccessor"/> represents a property of an open generic type.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a read-only property.
        /// <br/>-or-
        /// <br/>On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public void Set(object? instance, object? value, object? index)
        {
            try
            {
                ((Action<object?, object?, object?>)NonGenericSetter).Invoke(instance, value, index);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception
                PostValidate(instance, value, new[] { index }, e, true, false);
            }
        }

        /// <summary>
        /// Gets the value of the property.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// If the property is not an indexer, then <paramref name="indexParameters"/> parameter is omitted.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/> for static properties.</param>
        /// <param name="indexParameters">The parameters if the property is an indexer.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <para>Getting the property for the first time is slower than the <see cref="PropertyInfo.GetValue(object,object[])">System.Reflection.PropertyInfo.GetValue</see>
        /// method but further calls are much faster.</para>
        /// <note type="tip">If the property has no more than one index parameters and you know the type of the property at compile time
        /// (and also the declaring type for instance properties), then you can use the generic <see cref="GetStaticValue{TProperty}">GetStaticValue</see>
        /// or <see cref="O:KGySoft.Reflection.PropertyAccessor.GetInstanceValue">GetInstanceValue</see> methods for better performance.</note>
        /// <note type="caller">If the property is a non-<see langword="readonly"/> instance property of a value type, or has a pointer type or pointer index parameter,
        /// then the .NET Standard 2.0 version of this method defaults to use regular reflection
        /// to preserve possible mutations. To experience the best performance try to target .NET Standard 2.1 or any .NET Framework or .NET Core/.NET platforms instead.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">This <see cref="PropertyAccessor"/> represents an instance property and <paramref name="instance"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/>This <see cref="PropertyAccessor"/> represents an indexed property and <paramref name="indexParameters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> or one of the <paramref name="indexParameters"/> is invalid.
        /// <br/>-or-
        /// <br/><paramref name="indexParameters"/> has too few elements.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="PropertyAccessor"/> represents a property of an open generic type.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a write-only property.
        /// <br/>-or-
        /// <br/>On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        /// <overloads>The <see cref="Get(object, object[])"/> overload can be used for any number of index parameters.
        /// The other non-generic overloads can be used for simple properties or indexers with one parameter.
        /// <note type="tip">If you know the property type at compile time, then you can use the generic <see cref="GetStaticValue{TProperty}">GetStaticValue</see>
        /// overloads for static properties. If you know also the instance type (and the index parameter for indexers), then
        /// the <see cref="O:KGySoft.Reflection.PropertyAccessor.GetInstanceValue">GetInstanceValue</see>
        /// methods can be used to access instance properties with better performance.</note></overloads>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object? Get(object? instance, params object?[]? indexParameters)
        {
            try
            {
                return GeneralGetter.Invoke(instance, indexParameters);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(instance, null, indexParameters, e, false, true);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Gets the value of the property with no index parameters.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Get(object,object[])"/> overload for details.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/> for static properties.</param>
        /// <returns>The value of the property.</returns>
        /// <exception cref="ArgumentNullException">This <see cref="PropertyAccessor"/> represents an instance property and <paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> is invalid.
        /// <br/>-or-
        /// <br/>The property expects index parameters.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="PropertyAccessor"/> represents a property of an open generic type.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a write-only property.
        /// <br/>-or-
        /// <br/>On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object? Get(object? instance)
        {
            try
            {
                return ((Func<object?, object?>)NonGenericGetter).Invoke(instance);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(instance, null, Reflector.EmptyObjects, e, false, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Gets the value of the property with one index parameter.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="Get(object,object[])"/> overload for details.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/> for static properties.</param>
        /// <param name="index">The value of the index.</param>
        /// <returns>The value of the property.</returns>
        /// <exception cref="ArgumentNullException">This <see cref="PropertyAccessor"/> represents an instance property and <paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> or <paramref name="index"/> is invalid.
        /// <br/>-or-
        /// <br/>The property cannot be accessed with one index parameter.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="PropertyAccessor"/> represents a property of an open generic type.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a write-only property.
        /// <br/>-or-
        /// <br/>On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object? Get(object? instance, object? index)
        {
            try
            {
                return ((Func<object?, object?, object?>)NonGenericGetter).Invoke(instance, index);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(instance, null, new[] { index }, e, false, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Sets the strongly typed value of a static property. If the type of the property is not known at compile time
        /// the non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="value">The value to set.</param>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents an instance property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="TProperty"/> is invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a read-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetStaticValue<TProperty>(TProperty value)
        {
            if (GenericSetter is Action<TProperty> action)
                action.Invoke(value);
            else
                ThrowStatic<TProperty>();
        }

        /// <summary>
        /// Gets the strongly typed value of a static property. If the type of the property is not known at compile time
        /// the non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <returns>The value of the property.</returns>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents an instance property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentException"><typeparamref name="TProperty"/> is invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a write-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TProperty GetStaticValue<TProperty>() => GenericGetter is Func<TProperty> func ? func.Invoke() : ThrowStatic<TProperty>();

        /// <summary>
        /// Sets the strongly typed value of a non-indexed instance property in a reference type. If the type of the property or the declaring instance is not
        /// known at compile time the non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="instance">The instance that the property belongs to.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents a static property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a read-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void SetInstanceValue<TInstance, TProperty>(TInstance instance, TProperty value) where TInstance : class
        {
            if (GenericSetter is ReferenceTypeAction<TInstance, TProperty> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), value);
            else
                ThrowInstance<TProperty>();
        }

        /// <summary>
        /// Gets the strongly typed value of a non-indexed instance property in a reference type.
        /// If the type of the property or the declaring instance is not known at compile time the non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="instance">The instance that the property belongs to.</param>
        /// <returns>The value of the property.</returns>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents a static property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a write-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TProperty GetInstanceValue<TInstance, TProperty>(TInstance instance) where TInstance : class
            => GenericGetter is ReferenceTypeFunction<TInstance, TProperty> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance))
                : ThrowInstance<TProperty>();

        /// <summary>
        /// Sets the strongly typed value of a non-indexed instance property in a value type. If the type of the property or the declaring instance is not
        /// known at compile time the non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="instance">The instance that the property belongs to.</param>
        /// <param name="value">The value to set.</param>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents a static property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a read-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetInstanceValue<TInstance, TProperty>(in TInstance instance, TProperty value) where TInstance : struct
        {
            if (GenericSetter is ValueTypeAction<TInstance, TProperty> action)
                action.Invoke(instance, value);
            else
                ThrowInstance<TProperty>();
        }

        /// <summary>
        /// Gets the strongly typed value of a non-indexed instance property in a value type.
        /// If the type of the property or the declaring instance is not known at compile time the non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <param name="instance">The instance that the property belongs to.</param>
        /// <returns>The value of the property.</returns>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents a static property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a write-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TProperty GetInstanceValue<TInstance, TProperty>(in TInstance instance) where TInstance : struct
            => GenericGetter is ValueTypeFunction<TInstance, TProperty> func ? func.Invoke(instance) : ThrowInstance<TProperty>();

        /// <summary>
        /// Sets the strongly typed value of a single-parameter indexed property in a reference type. If the type of the property, the declaring instance or the index parameter is not
        /// known at compile time, or the indexer has more than one parameter, then the
        /// non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <typeparam name="TIndex">The type of the index parameter.</typeparam>
        /// <param name="instance">The instance that the property belongs to.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="index">The value of the index parameter.</param>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents a static property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a read-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void SetInstanceValue<TInstance, TProperty, TIndex>(TInstance instance, TProperty value, TIndex index) where TInstance : class
        {
            if (GenericSetter is ReferenceTypeAction<TInstance, TProperty, TIndex> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), value, index);
            else
                ThrowInstance<TProperty>();
        }

        /// <summary>
        /// Gets the strongly typed value of a single-parameter indexed property in a reference type.
        /// If the type of the property, the declaring instance or the index parameter is not known at compile time,
        /// or the indexer has more than one parameter, then the non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <typeparam name="TIndex">The type of the index parameter.</typeparam>
        /// <param name="instance">The instance that the property belongs to.</param>
        /// <param name="index">The value of the index parameter.</param>
        /// <returns>The value of the property.</returns>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents a static property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a write-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TProperty GetInstanceValue<TInstance, TProperty, TIndex>(TInstance instance, TIndex index) where TInstance : class
            => GenericGetter is ReferenceTypeFunction<TInstance, TIndex, TProperty> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), index)
                : ThrowInstance<TProperty>();

        /// <summary>
        /// Sets the strongly typed value of a single-parameter indexed property in a value type. If the type of the property,
        /// the declaring instance or the index parameter is not known at compile time, or the indexer has more than one parameter, then the
        /// non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <typeparam name="TIndex">The type of the index parameter.</typeparam>
        /// <param name="instance">The instance that the property belongs to.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="index">The value of the index parameter.</param>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents a static property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a read-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public void SetInstanceValue<TInstance, TProperty, TIndex>(in TInstance instance, TProperty value, TIndex index) where TInstance : struct
        {
            if (GenericSetter is ValueTypeAction<TInstance, TProperty, TIndex> action)
                action.Invoke(in instance, value, index);
            else
                ThrowInstance<TProperty>();
        }

        /// <summary>
        /// Gets the strongly typed value of a single-parameter indexed property in a value type.
        /// If the type of the property, the declaring instance or the index parameter is not known at compile time,
        /// or the indexer has more than one parameter, then the non-generic <see cref="O:KGySoft.Reflection.PropertyAccessor.Set">Set</see> methods can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the property.</typeparam>
        /// <typeparam name="TProperty">The type of the property.</typeparam>
        /// <typeparam name="TIndex">The type of the index parameter.</typeparam>
        /// <param name="instance">The instance that the property belongs to.</param>
        /// <param name="index">The value of the index parameter.</param>
        /// <returns>The value of the property.</returns>
        /// <exception cref="InvalidOperationException">This <see cref="PropertyAccessor"/> represents a static property or a property of an open generic type.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="NotSupportedException">This <see cref="PropertyAccessor"/> represents a write-only property
        /// or an indexed property with more than one parameter.</exception>
        /// <exception cref="PlatformNotSupportedException">You use the .NET Standard 2.0 build of <c>KGySoft.CoreLibraries</c> and this <see cref="PropertyAccessor"/>
        /// represents a <see langword="ref"/> property.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public TProperty GetInstanceValue<TInstance, TProperty, TIndex>(in TInstance instance, TIndex index) where TInstance : struct
            => GenericGetter is ValueTypeFunction<TInstance, TIndex, TProperty> func ? func.Invoke(instance, index) : ThrowInstance<TProperty>();

        #endregion

        #region Private Protected Methods

        private protected abstract Action<object?, object?, object?[]?> CreateGeneralSetter();
        private protected abstract Func<object?, object?[]?, object?> CreateGeneralGetter();
        private protected abstract Delegate CreateGenericSetter();
        private protected abstract Delegate CreateGenericGetter();
        private protected abstract Delegate CreateNonGenericSetter();
        private protected abstract Delegate CreateNonGenericGetter();

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ContractAnnotation("=> halt"), DoesNotReturn]
        private void PostValidate(object? instance, object? value, object?[]? indexParameters, Exception exception, bool isSetter, bool anyParams)
        {
            if (Property.DeclaringType?.ContainsGenericParameters == true)
                Throw.InvalidOperationException(Res.ReflectionGenericMember);

            if (!IsStatic)
            {
                if (instance == null)
                    Throw.ArgumentNullException(Argument.instance, Res.ReflectionInstanceIsNull);
                if (!Property.DeclaringType!.CanAcceptValue(instance))
                    Throw.ArgumentException(Argument.instance, Res.NotAnInstanceOfType(Property.DeclaringType!));
            }

            if (isSetter)
            {
                bool isByRef = Property.PropertyType.IsByRef;
                Type propertyType = isByRef ? Property.PropertyType.GetElementType()! : Property.PropertyType;
                if (propertyType.IsPointer)
                    propertyType = typeof(IntPtr);

                if (!propertyType.CanAcceptValue(value) || isByRef && value == null && propertyType.IsValueType)
                {
                    if (value == null)
                        Throw.ArgumentNullException(Argument.value, Res.NotAnInstanceOfType(propertyType));
                    Throw.ArgumentException(Argument.value, Res.NotAnInstanceOfType(propertyType));
                }
            }

            if (ParameterTypes.Length > 0)
            {
                if (indexParameters == null)
                {
                    Debug.Assert(anyParams);
                    Throw.ArgumentNullException(Argument.indexParameters, Res.ArgumentNull);
                }

                if (indexParameters.Length == 0 && anyParams)
                    Throw.ArgumentException(Argument.indexParameters, Res.ReflectionEmptyIndices);
                if (indexParameters.Length != ParameterTypes.Length)
                {
                    string message = Res.ReflectionIndexerParamsLengthMismatch(ParameterTypes.Length, indexParameters.Length);
                    if (anyParams)
                        Throw.ArgumentException(Argument.indexParameters, message);
                    else
                        Throw.ArgumentException(message);
                }

                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].CanAcceptValue(indexParameters[i]))
                    {
                        Type paramType = ParameterTypes[i];
                        if (paramType.IsPointer)
                            paramType = typeof(IntPtr);
                        if (anyParams)
                            Throw.ArgumentException(Argument.indexParameters, Res.ElementNotAnInstanceOfType(i, paramType));
                        else
                            Throw.ArgumentException("index", Res.NotAnInstanceOfType(paramType));
                    }
                }
            }

            ThrowIfSecurityConflict(exception);

            // exceptions from the property itself: re-throwing the original exception
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T ThrowStatic<T>() => !IsStatic
            ? Throw.InvalidOperationException<T>(Res.ReflectionStaticPropertyExpectedGeneric(Property.Name, Property.DeclaringType!))
            : Throw.ArgumentException<T>(Res.ReflectionCannotInvokePropertyGeneric(Property.Name, Property.DeclaringType));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T ThrowInstance<T>() => IsStatic
            ? Throw.InvalidOperationException<T>(Res.ReflectionInstancePropertyExpectedGeneric(Property.Name, Property.DeclaringType))
            : Throw.ArgumentException<T>(Res.ReflectionCannotInvokePropertyGeneric(Property.Name, Property.DeclaringType));

        #endregion

        #endregion

        #endregion
    }
}
