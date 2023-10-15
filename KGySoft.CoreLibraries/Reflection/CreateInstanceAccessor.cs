#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CreateInstanceAccessor.cs
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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

using KGySoft.Annotations;

using KGySoft.CoreLibraries;

#if !NET6_0_OR_GREATER
using KGySoft.CoreLibraries;
#endif

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Provides an efficient way for creating objects via dynamically created delegates.
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="CreateInstanceAccessor"/> instance by the static <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.GetAccessor">GetAccessor</see> methods.
    /// There are two overloads of them: <see cref="GetAccessor(Type)"/> can be used for types with parameterless constructors and for creating value types without a constructor,
    /// and the <see cref="GetAccessor(ConstructorInfo)"/> overload is for creating an instance by a specified constructor (with or without parameters).</para>
    /// <para>The <see cref="CreateInstance">CreateInstance</see> method can be used to create an actual instance of an object. It can be used even for constructors with parameters passed by reference.
    /// To obtain the result of possible <see langword="ref"/>/<see langword="out"/> parameters, pass a preallocated array to the <see cref="CreateInstance">CreateInstance</see> method.
    /// The parameters passed by reference will be assigned back to the corresponding array elements.</para>
    /// <para>If you know the created instance type and the parameter types at compile time, then you can use the
    /// generic <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.CreateInstance">CreateInstance</see> methods for better performance. These strongly typed methods can be used as
    /// long as the constructors to invoke have no more than four parameters and none of the parameters are passed by reference.</para>
    /// <para>The first call of these methods is slow because the delegate is generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to create an instance just by enlisting the constructor parameters of a <see cref="Type"/> rather than specifying a <see cref="ConstructorInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.CreateInstance">CreateInstance</see>
    /// methods in the <see cref="Reflector"/> class, which have some overloads for that purpose.</note>
    /// <note type="warning">The .NET Standard 2.0 version of the <see cref="CreateInstance">CreateInstance</see> does not return the ref/out parameters.
    /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
    /// <see cref="O:KGySoft.Reflection.Reflector.CreateInstance">Reflector.CreateInstance</see> methods to invoke constructors with ref/out parameters without losing the returned parameter values.</note>
    /// </remarks>
    /// <example><code lang="C#"><![CDATA[
    /// using System;
    /// using System.Reflection;
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
    ///     static void Main(string[] args)
    ///     {
    ///         Type testType = typeof(TestClass);
    ///         ConstructorInfo ctor = testType.GetConstructor(Type.EmptyTypes);
    ///         CreateInstanceAccessor accessor = CreateInstanceAccessor.GetAccessor(testType);
    /// 
    ///         new PerformanceTest<TestClass> { TestName = "Default Constructor", Iterations = 1_000_000 }
    ///             .AddCase(() => new TestClass(), "Direct call")
    ///             .AddCase(() => (TestClass)Activator.CreateInstance(testType), "Activator.CreateInstance")
    ///             .AddCase(() => (TestClass)ctor.Invoke(null), "ConstructorInfo.Invoke")
    ///             .AddCase(() => (TestClass)accessor.CreateInstance(), "CreateInstanceAccessor.CreateInstance")
    ///             .AddCase(() => accessor.CreateInstance<TestClass>(), "CreateInstanceAccessor.CreateInstance<>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    /// 
    ///         ctor = testType.GetConstructor(new[] { typeof(int) });
    ///         accessor = CreateInstanceAccessor.GetAccessor(ctor);
    ///         new PerformanceTest<TestClass> { TestName = "Parameterized Constructor", Iterations = 1_000_000 }
    ///             .AddCase(() => new TestClass(1), "Direct call")
    ///             .AddCase(() => (TestClass)Activator.CreateInstance(testType, 1), "Activator.CreateInstance")
    ///             .AddCase(() => (TestClass)ctor.Invoke(new object[] { 1 }), "ConstructorInfo.Invoke")
    ///             .AddCase(() => (TestClass)accessor.CreateInstance(1), "CreateInstanceAccessor.CreateInstance")
    ///             .AddCase(() => accessor.CreateInstance<TestClass, int>(1), "CreateInstanceAccessor.CreateInstance<,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // ==[Default Constructor Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 5
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct call: average time: 5.52 ms
    /// // 2. CreateInstanceAccessor.CreateInstance<>: average time: 9.35 ms (+3.82 ms / 169.17%)
    /// // 3. CreateInstanceAccessor.CreateInstance: average time: 9.64 ms(+4.12 ms / 174.58%)
    /// // 4. Activator.CreateInstance: average time: 14.36 ms(+8.83 ms / 259.85%)
    /// // 5. ConstructorInfo.Invoke: average time: 101.75 ms(+96.22 ms / 1,841.76%)
    /// // 
    /// // ==[Parameterized Constructor Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 5
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct call: average time: 5.03 ms
    /// // 2. CreateInstanceAccessor.CreateInstance<,>: average time: 9.91 ms (+4.88 ms / 197.08%)
    /// // 3. CreateInstanceAccessor.CreateInstance: average time: 19.62 ms(+14.59 ms / 390.25%)
    /// // 4. ConstructorInfo.Invoke: average time: 156.54 ms(+151.51 ms / 3,113.43%)
    /// // 5. Activator.CreateInstance: average time: 443.71 ms(+438.68 ms / 8,824.98%)]]></code>
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
                    if (ci.IsStatic)
                        Throw.ArgumentException(Argument.ctor, Res.ReflectionInstanceCtorExpected);
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
        /// <note type="tip">If the constructor has no more than four parameters and none of them are passed by reference, then you can use the generic
        /// <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.CreateInstance">CreateInstance</see> methods for better performance if the types are known at compile time.</note>
        /// <note type="caller">The .NET Standard 2.0 version of this method does not assign back the ref/out parameters in the <paramref name="parameters"/> argument.
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.CreateInstance">Reflector.CreateInstance</see> methods to invoke constructors with ref/out parameters without losing the returned parameter values.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">This <see cref="CreateInstanceAccessor"/> represents a constructor with parameters and <paramref name="parameters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of one of the <paramref name="parameters"/> is invalid.
        /// <br/>-or-
        /// <br/><paramref name="parameters"/> has too few elements.</exception>
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
                // Post-validation if there was any exception
                PostValidate( parameters, e, true);
                return null!; // actually never reached, just to satisfy the compiler
            }
        }

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
                // Post-validation if there was any exception
                PostValidate(Reflector.EmptyObjects, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

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
                // Post-validation if there was any exception
                PostValidate(new [] { param }, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

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
                // Post-validation if there was any exception
                PostValidate(new [] { param1, param2 }, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

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
                // Post-validation if there was any exception
                PostValidate(new [] { param1, param2, param3 }, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

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
                // Post-validation if there was any exception
                PostValidate(new [] { param1, param2, param3, param4 }, e, false);
                return null; // actually never reached, just to satisfy the compiler
            }
        }

        /// <summary>
        /// Creates a new instance using the associated <see cref="Type"/> or parameterless constructor.
        /// </summary>
        /// <typeparam name="TInstance">The type of the created instance.</typeparam>
        /// <returns>The created instance.</returns>
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters
        /// or a constructor that has parameters passed by reference.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TInstance CreateInstance<TInstance>()
            => GenericInitializer is Func<TInstance> func ? func.Invoke() : ThrowGeneric<TInstance>();

        /// <summary>
        /// Creates a new instance using the associated constructor with one parameter.
        /// </summary>
        /// <typeparam name="TInstance">The type of the created instance.</typeparam>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="param">The value of the parameter.</param>
        /// <returns>The created instance.</returns>
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters
        /// or a constructor that has parameters passed by reference.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
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
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters
        /// or a constructor that has parameters passed by reference.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
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
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters
        /// or a constructor that has parameters passed by reference.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
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
        /// <exception cref="NotSupportedException">This <see cref="CreateInstanceAccessor"/> represents a constructor with more than four parameters
        /// or a constructor that has parameters passed by reference.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
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
        private T ThrowGeneric<T>() => Throw.ArgumentException<T>(Res.ReflectionCannotCreateInstanceGeneric(MemberInfo is Type type ? type : MemberInfo.DeclaringType!));

        #endregion

        #endregion

        #endregion
    }
}
