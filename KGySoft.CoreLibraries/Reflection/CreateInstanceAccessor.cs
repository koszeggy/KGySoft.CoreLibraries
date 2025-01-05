#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CreateInstanceAccessor.cs
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
    /// Provides an efficient way for creating objects via dynamically created delegates.
    /// <div style="display: none;"><br/>See the <a href="https://docs.kgysoft.net/corelibraries/html/T_KGySoft_Reflection_CreateInstanceAccessor.htm">online help</a> for an example.</div>
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="CreateInstanceAccessor"/> instance by the static <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.GetAccessor">GetAccessor</see> methods.
    /// There are two overloads of them: <see cref="GetAccessor(Type)"/> can be used for types with parameterless constructors and for creating value types without a constructor,
    /// and the <see cref="GetAccessor(ConstructorInfo)"/> overload is for creating an instance by a specified constructor (with or without parameters).</para>
    /// <para>The <see cref="CreateInstance(object[])"/> method can be used to create an instance of an object in general cases.
    /// It can be used even for constructors with parameters passed by reference. To obtain the result of possible <see langword="ref"/>/<see langword="out"/>
    /// parameters, pass a preallocated array to the <see cref="CreateInstance(object[])"/> method.
    /// The parameters passed by reference will be assigned back to the corresponding array elements.</para>
    /// <para>The other non-generic <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.CreateInstance">CreateInstance</see> overloads can be used
    /// for constructors with no more than four parameters. They provide a better performance than the general <see cref="CreateInstance(object[])"/> method
    /// but the result of parameters passed reference will not be assigned back.</para>
    /// <para>If you know the type of the created instance and the parameter types at compile time, then you can use the
    /// generic <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.CreateInstance">CreateInstance</see> methods for even better performance.
    /// These strongly typed methods can be used as long as the constructors to invoke have no more than four parameters. Parameters passed by reference
    /// are also supported but only as input parameters as they are not assigned back to the caller.</para>
    /// <para>The first call of these methods is slower because the delegate is generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to create an instance just by enlisting the constructor parameters of a <see cref="Type"/> rather than specifying a <see cref="ConstructorInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.CreateInstance">CreateInstance</see>
    /// methods in the <see cref="Reflector"/> class, which have some overloads for that purpose.</note>
    /// </remarks>
    /// <example>
    /// The following example compares the <see cref="CreateInstanceAccessor"/> class with System.Reflection alternatives on .NET 8 and .NET Framework 4.8 platforms.
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
    ///         public TestClass() { }
    ///         public TestClass(int i) { }
    ///     }
    /// 
    ///     private static string PlatformName => ((TargetFrameworkAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(),
    ///         typeof(TargetFrameworkAttribute))).FrameworkDisplayName;
    /// 
    ///     static void Main()
    ///     {
    ///         Type testType = typeof(TestClass);
    ///         ConstructorInfo ctor = testType.GetConstructor(Type.EmptyTypes);
    ///         CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(testType);
    ///         
    ///         new PerformanceTest<TestClass> { TestName = $"Default Constructor - {PlatformName}", Iterations = 1_000_000 }
    ///             .AddCase(() => new TestClass(), "Direct call")
    ///             .AddCase(() => (TestClass)Activator.CreateInstance(testType), "System.Activator.CreateInstance")
    ///             .AddCase(() => (TestClass)ctor.Invoke(null), "System.Reflection.ConstructorInfo.Invoke")
    ///             .AddCase(() => (TestClass)accessor.CreateInstance(), "CreateInstanceAccessor.CreateInstance")
    ///             .AddCase(() => accessor.CreateInstance<TestClass>(), "CreateInstanceAccessor.CreateInstance<>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///         
    ///         ctor = testType.GetConstructor(new[] { typeof(int) });
    ///         accessor = CreateInstanceAccessor.GetAccessor(ctor);
    /// #if NET8_0_OR_GREATER
    ///         ConstructorInvoker invoker = ConstructorInvoker.Create(ctor);
    /// #endif
    ///         new PerformanceTest<TestClass> { TestName = $"Parameterized Constructor - {PlatformName}", Iterations = 1_000_000 }
    ///             .AddCase(() => new TestClass(1), "Direct call")
    ///             .AddCase(() => (TestClass)Activator.CreateInstance(testType, 1), "System.Activator.CreateInstance")
    ///             .AddCase(() => (TestClass)ctor.Invoke(new object[] { 1 }), "System.Reflection.ConstructorInfo.Invoke")
    /// #if NET8_0_OR_GREATER
    ///             .AddCase(() => (TestClass)invoker.Invoke(1), "System.Reflection.ConstructorInvoker.Invoke (.NET 8 or later)")
    /// #endif
    ///             .AddCase(() => (TestClass)accessor.CreateInstance(1), "CreateInstanceAccessor.CreateInstance")
    ///             .AddCase(() => accessor.CreateInstance<TestClass, int>(1), "CreateInstanceAccessor.CreateInstance<,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to these ones:
    /// 
    /// // ==[Default Constructor - .NET 8.0 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 5
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct call: average time: 8.66 ms
    /// // 2. CreateInstanceAccessor.CreateInstance<>: average time: 9.48 ms (+0.83 ms / 109.53%)
    /// // 3. CreateInstanceAccessor.CreateInstance: average time: 9.90 ms (+1.25 ms / 114.41%)
    /// // 4. System.Activator.CreateInstance: average time: 12.84 ms (+4.18 ms / 148.32%)
    /// // 5. System.Reflection.ConstructorInfo.Invoke: average time: 13.30 ms (+4.65 ms / 153.70%)
    /// //
    /// // ==[Parameterized Constructor - .NET 8.0 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 6
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct call: average time: 5.83 ms
    /// // 2. CreateInstanceAccessor.CreateInstance<,>: average time: 9.58 ms (+3.74 ms / 164.15%)
    /// // 3. CreateInstanceAccessor.CreateInstance: average time: 15.45 ms (+9.61 ms / 264.79%)
    /// // 4. System.Reflection.ConstructorInvoker.Invoke (.NET 8 or later): average time: 16.64 ms (+10.81 ms / 285.30%)
    /// // 5. System.Reflection.ConstructorInfo.Invoke: average time: 40.69 ms (+34.85 ms / 697.44%)
    /// // 6. System.Activator.CreateInstance: average time: 271.66 ms (+265.83 ms / 4,656.86%)
    /// 
    /// // ==[Default Constructor - .NET Framework 4.8 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 5
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct call: average time: 5.57 ms
    /// // 2. CreateInstanceAccessor.CreateInstance<>: average time: 14.13 ms (+8.56 ms / 253.57%)
    /// // 3. CreateInstanceAccessor.CreateInstance: average time: 32.48 ms (+26.91 ms / 582.84%)
    /// // 4. System.Activator.CreateInstance: average time: 48.24 ms (+42.67 ms / 865.77%)
    /// // 5. System.Reflection.ConstructorInfo.Invoke: average time: 204.87 ms (+199.30 ms / 3,676.54%)
    /// //
    /// // ==[Parameterized Constructor - .NET Framework 4.8 Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 5
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct call: average time: 5.43 ms
    /// // 2. CreateInstanceAccessor.CreateInstance<,>: average time: 14.28 ms (+8.85 ms / 263.06%)
    /// // 3. CreateInstanceAccessor.CreateInstance: average time: 18.96 ms (+13.53 ms / 349.15%)
    /// // 4. System.Reflection.ConstructorInfo.Invoke: average time: 199.26 ms (+193.83 ms / 3,669.64%)
    /// // 5. System.Activator.CreateInstance: average time: 682.53 ms (+677.10 ms / 12,569.87%)]]></code>
    /// </example>
    public abstract class CreateInstanceAccessor : MemberAccessor
    {
        #region Fields

        private Func<object?[]?, object>? generalInitializer;
        private Delegate? genericInitializer;
        private Delegate? nonGenericInitializer;

        #endregion

        #region Properties

        private protected Func<object?[]?, object> GeneralInitializer => generalInitializer ??= CreateGeneralInitializer();
        private protected Delegate GenericInitializer => genericInitializer ??= CreateGenericInitializer();
        private protected Delegate NonGenericInitializer => nonGenericInitializer ??= CreateNonGenericInitializer();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateInstanceAccessor"/> class.
        /// </summary>
        /// <param name="member">Can be a <see cref="Type"/> or a <see cref="ConstructorInfo"/>.</param>
        private protected CreateInstanceAccessor(MemberInfo member) :
            base(member, (member as ConstructorInfo)?.GetParameters().Select(p => p.ParameterType).ToArray())
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="CreateInstanceAccessor"/> for the specified <see cref="Type"/>.
        /// Given <paramref name="type"/> must have a parameterless constructor or must be a <see cref="ValueType"/>.
        /// </summary>
        /// <param name="type">A <see cref="Type"/> for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="CreateInstanceAccessor"/> instance that can be used to create an instance of <paramref name="type"/>.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static CreateInstanceAccessor GetAccessor(Type type)
        {
            if (type == null!)
                Throw.ArgumentNullException(Argument.type);
            return (CreateInstanceAccessor)GetCreateAccessor(type);
        }

        /// <summary>
        /// Gets a <see cref="CreateInstanceAccessor"/> for the specified <see cref="ConstructorInfo"/>.
        /// </summary>
        /// <param name="ctor">The constructor for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="CreateInstanceAccessor"/> instance that can be used to create an instance by the constructor.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static CreateInstanceAccessor GetAccessor(ConstructorInfo ctor)
        {
            if (ctor == null!)
                Throw.ArgumentNullException(Argument.ctor);
            return (CreateInstanceAccessor)GetCreateAccessor(ctor);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates an accessor for a constructor or type without caching.
        /// </summary>
        internal static CreateInstanceAccessor CreateAccessor(MemberInfo member)
        {
            switch (member)
            {
                case ConstructorInfo ci:
                    return new ParameterizedCreateInstanceAccessor(ci);
                case Type t:
#if NET6_0_OR_GREATER
                    return new DefaultCreateInstanceAccessor(t);
#else
                    // Pre.NET 6 platforms: Expression.New does not invoke structs with actual parameterless constructors so trying to obtain the default constructor in the first place
                    return t.GetDefaultConstructor() is ConstructorInfo defaultCtor ? new ParameterizedCreateInstanceAccessor(defaultCtor) : new DefaultCreateInstanceAccessor(t);
#endif
                default:
                    return Throw.ArgumentException<CreateInstanceAccessor>(Argument.member, Res.ReflectionTypeOrCtorInfoExpected);
            }
        }

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Creates a new instance by the associated <see cref="ConstructorInfo"/> or <see cref="Type"/>.
        /// For types and parameterless constructors the <paramref name="parameters"/> parameter is omitted.
        /// </summary>
        /// <param name="parameters">The parameters for parameterized constructors.</param>
        /// <returns>The created instance.</returns>
        /// <remarks>
        /// <para>Invoking the constructor for the first time is slower than the <see cref="MethodBase.Invoke(object,object[])">System.Reflection.MethodBase.Invoke</see>
        /// method but further calls are much faster.</para>
        /// <para>If the constructor has <see langword="ref"/>/<see langword="out"/> parameters pass a preallocated array to <paramref name="parameters"/>.
        /// The parameters passed by reference will be assigned back to the corresponding array elements.</para>
        /// <note type="tip">If the constructor has no more than four parameters, then you can use the generic
        /// <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.CreateInstance">CreateInstance</see> overloads for better performance if the types are known at compile time.</note>
        /// <note type="caller">If the constructor has <see langword="ref"/>/<see langword="out"/> parameters, then the .NET Standard 2.0 version of this method defaults
        /// to use regular reflection to be able to assign the parameter values back to the <paramref name="parameters"/> array.
        /// To experience the best performance try to target .NET Standard 2.1 or any .NET Framework or .NET Core/.NET platforms instead.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">This <see cref="CreateInstanceAccessor"/> represents a constructor with parameters and <paramref name="parameters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of one of the <paramref name="parameters"/> is invalid.
        /// <br/>-or-
        /// <br/><paramref name="parameters"/> has too few elements.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        /// <overloads>The <see cref="CreateInstance(object[])"/> overload can be used for any number of parameters or for constructors
        /// with <see langword="ref"/>/<see langword="out"/> parameters. The other non-generic overloads can be used for constructors with no more than four parameters.
        /// And if you know the type of the created instance and the parameter types at compile time, then you can use the
        /// generic overloads for the best performance.</overloads>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object CreateInstance(params object?[]? parameters)
        {
            try
            {
                return GeneralInitializer.Invoke(parameters);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate( parameters, e, true);
                return null!; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Creates a new instance using the associated <see cref="Type"/> or parameterless constructor.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="CreateInstance(object[])"/> overload for details.
        /// </summary>
        /// <returns>The created instance.</returns>
        /// <exception cref="ArgumentException">The constructor expects parameters.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object CreateInstance()
        {
            try
            {
                return ((Func<object>)NonGenericInitializer).Invoke();
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(Reflector.EmptyObjects, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Creates a new instance using the associated constructor with one parameter.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="CreateInstance(object[])"/> overload for details.
        /// </summary>
        /// <param name="param">The value of the parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="ArgumentException">The type of the specified parameter is invalid.
        /// <br/>-or-
        /// <br/>The constructor cannot be invoked with one parameter.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object CreateInstance(object? param)
        {
            try
            {
                return ((Func<object?, object>)NonGenericInitializer).Invoke(param);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(new [] { param }, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Creates a new instance using the associated constructor with two parameters.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="CreateInstance(object[])"/> overload for details.
        /// </summary>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="ArgumentException">The type of a specified parameter is invalid.
        /// <br/>-or-
        /// <br/>The constructor cannot be invoked with two parameters.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object CreateInstance(object? param1, object? param2)
        {
            try
            {
                return ((Func<object?, object?, object>)NonGenericInitializer).Invoke(param1, param2);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(new [] { param1, param2 }, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Creates a new instance using the associated constructor with three parameters.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="CreateInstance(object[])"/> overload for details.
        /// </summary>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="ArgumentException">The type of a specified parameter is invalid.
        /// <br/>-or-
        /// <br/>The constructor cannot be invoked with three parameters.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object CreateInstance(object? param1, object? param2, object? param3)
        {
            try
            {
                return ((Func<object?, object?, object?, object>)NonGenericInitializer).Invoke(param1, param2, param3);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(new [] { param1, param2, param3 }, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Creates a new instance using the associated constructor with four parameters.
        /// <br/>See the <strong>Remarks</strong> section of the <see cref="CreateInstance(object[])"/> overload for details.
        /// </summary>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <param name="param4">The value of the fourth parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="ArgumentException">The type of a specified parameter is invalid.
        /// <br/>-or-
        /// <br/>The constructor cannot be invoked with four parameters.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        /// <exception cref="NotSupportedException">On .NET Framework the code is executed in a partially trusted domain with insufficient permissions.</exception>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        [SuppressMessage("Design", "CA1031:Do not catch general exception types",
            Justification = "False alarm, exception is re-thrown but the analyzer fails to consider the [DoesNotReturn] attribute")]
        public object CreateInstance(object? param1, object? param2, object? param3, object? param4)
        {
            try
            {
                return ((Func<object?, object?, object?, object?, object>)NonGenericInitializer).Invoke(param1, param2, param3, param4);
            }
            catch (Exception e)
            {
                // Post-validation if there was any exception. We do this for better performance on the happy path.
                PostValidate(new [] { param1, param2, param3, param4 }, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Creates a new instance using the associated <see cref="Type"/> or parameterless constructor.
        /// </summary>
        /// <typeparam name="TInstance">The type of the created instance.</typeparam>
        /// <returns>The created instance.</returns>
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        public TInstance CreateInstance<TInstance>()
            => GenericInitializer is Func<TInstance> func ? func.Invoke() : ThrowGeneric<TInstance>();

        /// <summary>
        /// Creates a new instance using the associated constructor with one parameter.
        /// </summary>
        /// <typeparam name="TInstance">The type of the created instance.</typeparam>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="param">The value of the parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        public TInstance CreateInstance<TInstance, T>(T param)
            => GenericInitializer is Func<T, TInstance> func ? func.Invoke(param) : ThrowGeneric<TInstance>();

        /// <summary>
        /// Creates a new instance using the associated constructor with two parameters.
        /// </summary>
        /// <typeparam name="TInstance">The type of the created instance.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        public TInstance CreateInstance<TInstance, T1, T2>(T1 param1, T2 param2)
            => GenericInitializer is Func<T1, T2, TInstance> func ? func.Invoke(param1, param2) : ThrowGeneric<TInstance>();

        /// <summary>
        /// Creates a new instance using the associated constructor with three parameters.
        /// </summary>
        /// <typeparam name="TInstance">The type of the created instance.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        public TInstance CreateInstance<TInstance, T1, T2, T3>(T1 param1, T2 param2, T3 param3)
            => GenericInitializer is Func<T1, T2, T3, TInstance> func ? func.Invoke(param1, param2, param3) : ThrowGeneric<TInstance>();

        /// <summary>
        /// Creates a new instance using the associated constructor with four parameters.
        /// </summary>
        /// <typeparam name="TInstance">The type of the created instance.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <param name="param4">The value of the fourth parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="CreateInstanceAccessor"/> represents a static constructor, a constructor of an abstract class,
        /// or.a constructor of an open generic type.</exception>
        public TInstance CreateInstance<TInstance, T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4)
            => GenericInitializer is Func<T1, T2, T3, T4, TInstance> func ? func.Invoke(param1, param2, param3, param4) : ThrowGeneric<TInstance>();

        #endregion

        #region Private Protected Methods

        private protected abstract Func<object?[]?, object> CreateGeneralInitializer();
        private protected abstract Delegate CreateGenericInitializer();
        private protected abstract Delegate CreateNonGenericInitializer();

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ContractAnnotation("=> halt"), DoesNotReturn]
        private void PostValidate(object?[]? parameters, Exception exception, bool anyParams)
        {
            // These could be just re-thrown at the end but we want to avoid parameter checks for them
            Type? type = MemberInfo as Type ?? (MemberInfo as ConstructorInfo)?.DeclaringType;
            if (type is null || MemberInfo is ConstructorInfo { IsStatic: true })
                Throw.InvalidOperationException(Res.ReflectionInstanceCtorExpected);
            if (type.IsAbstract || type.ContainsGenericParameters)
                Throw.InvalidOperationException(Res.ReflectionCannotCreateInstanceOfType(type));

            if (ParameterTypes.Length > 0)
            {
                if (parameters == null)
                {
                    Debug.Assert(anyParams);
                    Throw.ArgumentNullException(Argument.parameters, Res.ArgumentNull);
                }

                if (parameters.Length != ParameterTypes.Length)
                {
                    string message = Res.ReflectionParamsLengthMismatch(ParameterTypes.Length, parameters.Length);
                    if (anyParams)
                        Throw.ArgumentException(Argument.parameters, message);
                    else
                        Throw.ArgumentException(message);
                }

                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].CanAcceptValue(parameters[i]))
                    {
                        if (anyParams)
                            Throw.ArgumentException(Argument.parameters, Res.ElementNotAnInstanceOfType(i, ParameterTypes[i]));
                        else
                            Throw.ArgumentException($"param{(ParameterTypes.Length > 1 ? i + 1 : null)}", Res.NotAnInstanceOfType(ParameterTypes[i]));
                    }
                }
            }

            ThrowIfSecurityConflict(exception);

            // exceptions from the method itself: re-throwing the original exception
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T ThrowGeneric<T>() => Throw.ArgumentException<T>(Res.ReflectionCannotCreateInstanceGeneric(MemberInfo as Type ?? MemberInfo.DeclaringType!));

        #endregion

        #endregion

        #endregion
    }
}
