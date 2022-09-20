#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MethodAccessor.cs
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
    /// Provides an efficient way for invoking methods via dynamically created delegates.
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="MethodAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.</para>
    /// <para>The <see cref="Invoke">Invoke</see> method can be used to invoke the method. It can be used even for methods with parameters passed by reference.
    /// To obtain the result of possible <see langword="ref"/>/<see langword="out"/> parameters, pass a preallocated array to the <see cref="Invoke">Invoke</see> method.
    /// The parameters passed by reference will be assigned back to the corresponding array elements.</para>
    /// <para>If you know the parameter types at compile time (and the return type for function methods), then you can use
    /// the <see cref="O:KGySoft.Reflection.MethodAccessor.InvokeStaticAction">InvokeStaticAction</see>/<see cref="O:KGySoft.Reflection.MethodAccessor.InvokeStaticFunction">InvokeStaticFunction</see>
    /// methods to invoke static methods. If you know also the instance type, then
    /// the <see cref="O:KGySoft.Reflection.MethodAccessor.InvokeInstanceAction">InvokeInstanceAction</see>/<see cref="O:KGySoft.Reflection.MethodAccessor.InvokeInstanceFunction">InvokeInstanceFunction</see>
    /// methods can be used to invoke instance methods for better performance. These strongly typed methods can be used as
    /// long as the methods to invoke have no more than four parameters and none of the parameters are passed by reference.</para>
    /// <para>The first call of these methods are slow because the delegates are generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to invoke a method by name rather then by a <see cref="MethodInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.InvokeMethod">InvokeMethod</see>
    /// methods in the <see cref="Reflector"/> class, which have some overloads with a <c>methodName</c> parameter.</note>
    /// <note type="warning">The .NET Standard 2.0 version of the <see cref="Invoke">Invoke</see> method does not return the ref/out parameters.
    /// Furthermore, if an instance method of a value type (<see langword="struct"/>) mutates the instance,
    /// then the changes will not be applied to the instance on which the method is invoked.
    /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
    /// <see cref="O:KGySoft.Reflection.Reflector.InvokeMethod">Reflector.InvokeMethod</see> overloads to invoke methods with ref/out parameters without losing the returned parameter values
    /// and to preserve changes of the mutated value type instances.</note>
    /// </remarks>
    /// <example>
    /// <code lang="C#"><![CDATA[
    /// using System;
    /// using System.Reflection;
    /// using KGySoft.Diagnostics;
    /// using KGySoft.Reflection;
    /// 
    /// class Example
    /// {
    ///     private class TestClass
    ///     {
    ///         public int TestMethod(int i) => i;
    ///     }
    /// 
    ///     static void Main(string[] args)
    ///     {
    ///         var instance = new TestClass();
    ///         MethodInfo method = instance.GetType().GetMethod(nameof(TestClass.TestMethod));
    ///         MethodAccessor accessor = MethodAccessor.GetAccessor(method);
    /// 
    ///         new PerformanceTest { Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestMethod(1), "Direct call")
    ///             .AddCase(() => method.Invoke(instance, new object[] { 1 }), "MethodInfo.Invoke")
    ///             .AddCase(() => accessor.Invoke(instance, 1), "MethodAccessor.Invoke")
    ///             .AddCase(() => accessor.InvokeInstanceFunction<TestClass, int, int>(instance, 1), "MethodAccessor.InvokeInstanceFunction<,,>")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // ==[Performance Test Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 4
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct call: average time: 3.23 ms
    /// // 2. MethodAccessor.InvokeInstanceFunction<,,>: average time: 5.72 ms (+2.49 ms / 177.25%)
    /// // 3. MethodAccessor.Invoke: average time: 18.96 ms(+15.73 ms / 587.38%)
    /// // 4. MethodInfo.Invoke: average time: 155.54 ms(+152.31 ms / 4,819.52%)]]></code>
    /// </example>
    public abstract class MethodAccessor : MemberAccessor
    {
        #region Fields

        private Delegate? invoker;
        private Delegate? genericInvoker;

        #endregion

        #region Properties

        private protected MethodBase Method => (MethodBase)MemberInfo;
        private protected Delegate Invoker => invoker ??= CreateInvoker();
        private protected Delegate GenericInvoker => genericInvoker ??= CreateGenericInvoker();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodAccessor"/> class.
        /// </summary>
        /// <param name="method">The method for which the accessor is to be created.</param>
        private protected MethodAccessor(MethodBase method) :
            // ReSharper disable once ConstantConditionalAccessQualifier - null check is in base so it is needed here
            base(method, method?.GetParameters().Select(p => p.ParameterType).ToArray())
        {
        }

        #endregion

        #region Methods

        #region Static Methods

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="MemberAccessor"/> for the specified <paramref name="method"/>.
        /// </summary>
        /// <param name="method">The method for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="MethodAccessor"/> instance that can be used to invoke the method.</returns>
        [MethodImpl(MethodImpl.AggressiveInlining)]
        public static MethodAccessor GetAccessor(MethodInfo method)
        {
            if (method == null!)
                Throw.ArgumentNullException(Argument.method);
            return (MethodAccessor)GetCreateAccessor(method);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates an accessor for a method without caching.
        /// </summary>
        /// <param name="method">The method for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="MethodAccessor"/> instance that can be used to invoke the method.</returns>
        internal static MethodAccessor CreateAccessor(MethodInfo method) => method.ReturnType == Reflector.VoidType
            ? new ActionMethodAccessor(method)
            : new FunctionMethodAccessor(method);

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Invokes the method. The return value of <see langword="void"/> methods is always <see langword="null"/>.
        /// For static methods the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// </summary>
        /// <param name="instance">The instance that the method belongs to. Can be <see langword="null"/> for static methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.
        /// If the method has ref/out parameters the corresponding array elements are assigned back with the results.</param>
        /// <returns>The return value of the method, or <see langword="null"/> for <see langword="void"/> methods.</returns>
        /// <remarks>
        /// <para>Invoking the method for the first time is slower than the <see cref="MethodBase.Invoke(object,object[])">System.Reflection.MethodBase.Invoke</see>
        /// method but further calls are much faster.</para>
        /// <note type="tip">If the method has no more than four parameters and none of them are passed by reference, then you can use the strongly typed
        /// <see cref="O:KGySoft.Reflection.MethodAccessor.InvokeStaticAction">InvokeStaticAction</see>, <see cref="O:KGySoft.Reflection.MethodAccessor.InvokeStaticFunction">InvokeStaticFunction</see>,
        /// <see cref="O:KGySoft.Reflection.MethodAccessor.InvokeInstanceAction">InvokeInstanceAction</see> or <see cref="O:KGySoft.Reflection.MethodAccessor.InvokeInstanceFunction">InvokeInstanceFunction</see>
        /// methods for better performance if the types are known at compile time.</note>
        /// <note type="caller">The .NET Standard 2.0 version of this method does not assign back the ref/out parameters in the <paramref name="parameters"/> argument.
        /// Furthermore, if an instance method of a value type (<see langword="struct"/>) mutates the instance,
        /// then the changes will not be applied to the <paramref name="instance"/> parameter in the .NET Standard 2.0 version.
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.InvokeMethod">Reflector.InvokeMethod</see> overloads to invoke methods with ref/out parameters without losing the returned parameter values
        /// and to preserve changes of the mutated value type instances.</note>
        /// </remarks>
        /// <exception cref="ArgumentNullException">This <see cref="MethodAccessor"/> represents an instance method and <paramref name="instance"/> is <see langword="null"/>
        /// <br/>-or-
        /// <br/>This <see cref="MethodAccessor"/> represents a method with parameters and <paramref name="parameters"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The type of <paramref name="instance"/> or one of the <paramref name="parameters"/> is invalid.
        /// <br/>-or-
        /// <br/><paramref name="parameters"/> has too few elements.</exception>
        public abstract object? Invoke(object? instance, params object?[]? parameters);

        /// <summary>
        /// Invokes a parameterless static action method.
        /// </summary>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">This <see cref="MethodAccessor"/> does not represent a parameterless action method so
        /// type arguments should be specified (use the generic invoker method with matching type arguments).</exception>
        public void InvokeStaticAction()
        {
            if (GenericInvoker is Action action)
                action.Invoke();
            else
                ThrowStatic<_>();
        }

        /// <summary>
        /// Invokes a static action method with one parameter. If the type of the parameter is not known at compile time
        /// the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="param">The value of the parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeStaticAction<T>(T param)
        {
            if (GenericInvoker is Action<T> action)
                action.Invoke(param);
            else
                ThrowStatic<_>();
        }

        /// <summary>
        /// Invokes a static action method with two parameters. If the type of the parameters are not known at compile time
        /// the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeStaticAction<T1, T2>(T1 param1, T2 param2)
        {
            if (GenericInvoker is Action<T1, T2> action)
                action.Invoke(param1, param2);
            else
                ThrowStatic<_>();
        }

        /// <summary>
        /// Invokes a static action method with three parameters. If the type of the parameters are not known at compile time
        /// the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeStaticAction<T1, T2, T3>(T1 param1, T2 param2, T3 param3)
        {
            if (GenericInvoker is Action<T1, T2, T3> action)
                action.Invoke(param1, param2, param3);
            else
                ThrowStatic<_>();
        }

        /// <summary>
        /// Invokes a static action method with four parameters. If the type of the parameters are not known at compile time
        /// the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <param name="param4">The value of the fourth parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeStaticAction<T1, T2, T3, T4>(T1 param1, T2 param2, T3 param3, T4 param4)
        {
            if (GenericInvoker is Action<T1, T2, T3, T4> action)
                action.Invoke(param1, param2, param3, param4);
            else
                ThrowStatic<_>();
        }

        /// <summary>
        /// Invokes a parameterless static function method. If the return type is not known at compile time
        /// the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeStaticFunction<TResult>()
            => GenericInvoker is Func<TResult> func ? func.Invoke() : ThrowStatic<TResult>();

        /// <summary>
        /// Invokes a static function method with one parameter. If the type of the parameter or the return value
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="param">The value of the parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeStaticFunction<T, TResult>(T param)
            => GenericInvoker is Func<T, TResult> func ? func.Invoke(param) : ThrowStatic<TResult>();

        /// <summary>
        /// Invokes a static function method with two parameters. If the type of the parameters or the return value
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeStaticFunction<T1, T2, TResult>(T1 param1, T2 param2)
            => GenericInvoker is Func<T1, T2, TResult> func ? func.Invoke(param1, param2) : ThrowStatic<TResult>();

        /// <summary>
        /// Invokes a static function method with three parameters. If the type of the parameters or the return value
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeStaticFunction<T1, T2, T3, TResult>(T1 param1, T2 param2, T3 param3)
            => GenericInvoker is Func<T1, T2, T3, TResult> func ? func.Invoke(param1, param2, param3) : ThrowStatic<TResult>();

        /// <summary>
        /// Invokes a static function method with four parameters. If the type of the parameters or the return value
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <param name="param4">The value of the fourth parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents an instance method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeStaticFunction<T1, T2, T3, T4, TResult>(T1 param1, T2 param2, T3 param3, T4 param4)
            => GenericInvoker is Func<T1, T2, T3, T4, TResult> func ? func.Invoke(param1, param2, param3, param4) : ThrowStatic<TResult>();

        /// <summary>
        /// Invokes a parameterless instance action method in a reference type. If the type of the declaring instance
        /// is not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void InvokeInstanceAction<TInstance>(TInstance instance) where TInstance : class
        {
            if (GenericInvoker is ReferenceTypeAction<TInstance> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance));
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes an instance action method with one parameter in a reference type. If the type of the parameter or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param">The value of the parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void InvokeInstanceAction<TInstance, T>(TInstance instance, T param) where TInstance : class
        {
            if (GenericInvoker is ReferenceTypeAction<TInstance, T> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), param);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes an instance action method with two parameters in a reference type. If the type of the parameters or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void InvokeInstanceAction<TInstance, T1, T2>(TInstance instance, T1 param1, T2 param2) where TInstance : class
        {
            if (GenericInvoker is ReferenceTypeAction<TInstance, T1, T2> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), param1, param2);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes an instance action method with three parameters in a reference type. If the type of the parameters or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void InvokeInstanceAction<TInstance, T1, T2, T3>(TInstance instance, T1 param1, T2 param2, T3 param3) where TInstance : class
        {
            if (GenericInvoker is ReferenceTypeAction<TInstance, T1, T2, T3> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), param1, param2, param3);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes an instance action method with four parameters in a reference type. If the type of the parameters or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <param name="param4">The value of the fourth parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public void InvokeInstanceAction<TInstance, T1, T2, T3, T4>(TInstance instance, T1 param1, T2 param2, T3 param3, T4 param4) where TInstance : class
        {
            if (GenericInvoker is ReferenceTypeAction<TInstance, T1, T2, T3, T4> action)
                action.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), param1, param2, param3, param4);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes a parameterless instance function method in a reference type. If the type of the return value or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TResult InvokeInstanceFunction<TInstance, TResult>(TInstance instance) where TInstance : class
            => GenericInvoker is ReferenceTypeFunction<TInstance, TResult> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance))
                : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes an instance function method with one parameter in a reference type. If the type of the parameter, the return value
        /// or the declaring instance are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param">The value of the parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TResult InvokeInstanceFunction<TInstance, T, TResult>(TInstance instance, T param) where TInstance : class
            => GenericInvoker is ReferenceTypeFunction<TInstance, T, TResult> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), param)
                : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes an instance function method with two parameters in a reference type. If the type of the parameters, the return value
        /// or the declaring instance are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TResult InvokeInstanceFunction<TInstance, T1, T2, TResult>(TInstance instance, T1 param1, T2 param2) where TInstance : class
            => GenericInvoker is ReferenceTypeFunction<TInstance, T1, T2, TResult> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), param1, param2)
                : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes an instance function method with three parameters in a reference type. If the type of the parameters, the return value
        /// or the declaring instance are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TResult InvokeInstanceFunction<TInstance, T1, T2, T3, TResult>(TInstance instance, T1 param1, T2 param2, T3 param3) where TInstance : class
            => GenericInvoker is ReferenceTypeFunction<TInstance, T1, T2, T3, TResult> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), param1, param2, param3)
                : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes an instance function method with four parameters in a reference type. If the type of the parameters, the return value
        /// or the declaring instance are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <param name="param4">The value of the fourth parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        [SuppressMessage("ReSharper", "ConstantNullCoalescingCondition", Justification = "False alarm, instance CAN be null even though it MUST NOT be null.")]
        public TResult InvokeInstanceFunction<TInstance, T1, T2, T3, T4, TResult>(TInstance instance, T1 param1, T2 param2, T3 param3, T4 param4) where TInstance : class
            => GenericInvoker is ReferenceTypeFunction<TInstance, T1, T2, T3, T4, TResult> func
                ? func.Invoke(instance ?? Throw.ArgumentNullException<TInstance>(Argument.instance), param1, param2, param3, param4)
                : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes a parameterless instance action method in a value type. If the type of the declaring instance
        /// is not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeInstanceAction<TInstance>(in TInstance instance) where TInstance : struct
        {
            if (GenericInvoker is ValueTypeAction<TInstance> action)
                action.Invoke(instance);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes an instance action method with one parameter in a value type. If the type of the parameter or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param">The value of the parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeInstanceAction<TInstance, T>(in TInstance instance, T param) where TInstance : struct
        {
            if (GenericInvoker is ValueTypeAction<TInstance, T> action)
                action.Invoke(instance, param);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes an instance action method with two parameters in a value type. If the type of the parameters or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeInstanceAction<TInstance, T1, T2>(in TInstance instance, T1 param1, T2 param2) where TInstance : struct
        {
            if (GenericInvoker is ValueTypeAction<TInstance, T1, T2> action)
                action.Invoke(instance, param1, param2);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes an instance action method with three parameters in a value type. If the type of the parameters or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeInstanceAction<TInstance, T1, T2, T3>(in TInstance instance, T1 param1, T2 param2, T3 param3) where TInstance : struct
        {
            if (GenericInvoker is ValueTypeAction<TInstance, T1, T2, T3> action)
                action.Invoke(instance, param1, param2, param3);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes an instance action method with four parameters in a value type. If the type of the parameters or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <param name="param4">The value of the fourth parameter.</param>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public void InvokeInstanceAction<TInstance, T1, T2, T3, T4>(in TInstance instance, T1 param1, T2 param2, T3 param3, T4 param4) where TInstance : struct
        {
            if (GenericInvoker is ValueTypeAction<TInstance, T1, T2, T3, T4> action)
                action.Invoke(instance, param1, param2, param3, param4);
            else
                ThrowInstance<_>();
        }

        /// <summary>
        /// Invokes a parameterless instance function method in a value type. If the type of the return value or the declaring instance
        /// are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeInstanceFunction<TInstance, TResult>(in TInstance instance) where TInstance : struct
            => GenericInvoker is ValueTypeFunction<TInstance, TResult> func ? func.Invoke(instance) : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes an instance function method with one parameter in a value type. If the type of the parameter, the return value
        /// or the declaring instance are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param">The value of the parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeInstanceFunction<TInstance, T, TResult>(in TInstance instance, T param) where TInstance : struct
            => GenericInvoker is ValueTypeFunction<TInstance, T, TResult> func ? func.Invoke(instance, param) : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes an instance function method with two parameters in a value type. If the type of the parameters, the return value
        /// or the declaring instance are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeInstanceFunction<TInstance, T1, T2, TResult>(in TInstance instance, T1 param1, T2 param2) where TInstance : struct
            => GenericInvoker is ValueTypeFunction<TInstance, T1, T2, TResult> func ? func.Invoke(instance, param1, param2) : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes an instance function method with three parameters in a value type. If the type of the parameters, the return value
        /// or the declaring instance are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeInstanceFunction<TInstance, T1, T2, T3, TResult>(in TInstance instance, T1 param1, T2 param2, T3 param3) where TInstance : struct
            => GenericInvoker is ValueTypeFunction<TInstance, T1, T2, T3, TResult> func ? func.Invoke(instance, param1, param2, param3) : ThrowInstance<TResult>();

        /// <summary>
        /// Invokes an instance function method with four parameters in a value type. If the type of the parameters, the return value
        /// or the declaring instance are not known at compile time the non-generic <see cref="Invoke">Invoke</see> method can be used.
        /// </summary>
        /// <typeparam name="TInstance">The type of the instance that declares the method.</typeparam>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="TResult">The return type of the method.</typeparam>
        /// <param name="instance">The instance that the method belongs to.</param>
        /// <param name="param1">The value of the first parameter.</param>
        /// <param name="param2">The value of the second parameter.</param>
        /// <param name="param3">The value of the third parameter.</param>
        /// <param name="param4">The value of the fourth parameter.</param>
        /// <returns>The return value of the method.</returns>
        /// <exception cref="NotSupportedException">This <see cref="MethodAccessor"/> represents a method with more than four parameters
        /// or a method that has parameters passed by reference.</exception>
        /// <exception cref="InvalidOperationException">This <see cref="MethodAccessor"/> represents a static method.</exception>
        /// <exception cref="ArgumentException">The number or types of the type arguments are invalid.</exception>
        public TResult InvokeInstanceFunction<TInstance, T1, T2, T3, T4, TResult>(in TInstance instance, T1 param1, T2 param2, T3 param3, T4 param4) where TInstance : struct
            => GenericInvoker is ValueTypeFunction<TInstance, T1, T2, T3, T4, TResult> func ? func.Invoke(instance, param1, param2, param3, param4) : ThrowInstance<TResult>();

        #endregion

        #region Private Protected Methods

        private protected abstract Delegate CreateInvoker();
        private protected abstract Delegate CreateGenericInvoker();

        [MethodImpl(MethodImplOptions.NoInlining)]
        [ContractAnnotation("=> halt"), DoesNotReturn]
        private protected void PostValidate(object? instance, object?[]? parameters, Exception exception)
        {
            if (!Method.IsStatic)
            {
                if (instance == null)
                    Throw.ArgumentNullException(Argument.instance, Res.ReflectionInstanceIsNull);
                if (!Method.DeclaringType!.CanAcceptValue(instance))
                    Throw.ArgumentException(Argument.instance, Res.NotAnInstanceOfType(Method.DeclaringType!));
            }

            if (ParameterTypes.Length > 0)
            {
                if (parameters == null)
                    Throw.ArgumentNullException(Argument.parameters, Res.ArgumentNull);
                if (parameters.Length < ParameterTypes.Length)
                    Throw.ArgumentException(Argument.parameters, Res.ReflectionParametersInvalid);
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    if (!ParameterTypes[i].CanAcceptValue(parameters[i]))
                        Throw.ArgumentException(Argument.parameters, Res.ReflectionParametersInvalid);
                }
            }

            ThrowIfSecurityConflict(exception);

            // exceptions from the method itself: re-throwing the original exception
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T ThrowStatic<T>() => !Method.IsStatic
            ? Throw.InvalidOperationException<T>(Res.ReflectionStaticMethodExpectedGeneric(Method.Name, Method.DeclaringType!))
            : Throw.ArgumentException<T>(Res.ReflectionCannotInvokeMethodGeneric(Method.Name, Method.DeclaringType));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T ThrowInstance<T>() => Method.IsStatic
            ? Throw.InvalidOperationException<T>(Res.ReflectionInstanceMethodExpectedGeneric(Method.Name, Method.DeclaringType))
            : Throw.ArgumentException<T>(Res.ReflectionCannotInvokeMethodGeneric(Method.Name, Method.DeclaringType));

        #endregion

        #endregion

        #endregion
    }
}
