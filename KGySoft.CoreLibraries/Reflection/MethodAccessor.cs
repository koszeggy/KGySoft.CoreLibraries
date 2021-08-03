#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: MethodAccessor.cs
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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Provides an efficient way for invoking methods via dynamically created delegates.
    /// <br/>See the <strong>Remarks</strong> section for details and an example.
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="MethodAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.</para>
    /// <para>The <see cref="Invoke">Invoke</see> method can be used to invoke the method.
    /// The first call of this method is slow because the delegate is generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to invoke a method by name rather then by a <see cref="MethodInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.InvokeMethod">InvokeMethod</see>
    /// methods in the <see cref="Reflector"/> class, which have some overloads with a <c>propertyName</c> parameter.</note>
    /// <note type="warning">The .NET Standard 2.0 version of the <see cref="Invoke">Invoke</see> method does not return the ref/out parameters.
    /// Furthermore, if an instance method of a value type (<see langword="struct"/>) mutates the instance,
    /// then the changes will not be applied to the instance on which the method is invoked.
    /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
    /// <see cref="O:KGySoft.Reflection.Reflector.InvokeMethod">Reflector.InvokeMethod</see> overloads to invoke methods with ref/out parameters without losing the returned parameter values
    /// and to preserve changes the of the mutated value type instances.</note>
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
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // ==[Performance Test Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 3
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct call: average time: 2.87 ms
    /// // 2. MethodAccessor.Invoke: average time: 26.02 ms (+23.15 ms / 906.97 %)
    /// // 3. MethodInfo.Invoke: average time: 241.47 ms (+238.60 ms / 8,416.44 %)]]></code>
    /// </example>
    public abstract class MethodAccessor : MemberAccessor
    {
        #region Fields

        private Delegate? invoker;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the method invoker delegate.
        /// </summary>
        private protected Delegate Invoker
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get => invoker ??= CreateInvoker();
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodAccessor"/> class.
        /// </summary>
        /// <param name="method">The method for which the accessor is to be created.</param>
        protected MethodAccessor(MethodBase method) :
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
            if (method == null)
                Throw.ArgumentNullException(Argument.method);
            return (MethodAccessor)GetCreateAccessor(method);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates an accessor for a property without caching.
        /// </summary>
        /// <param name="method">The method for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="MethodAccessor"/> instance that can be used to invoke the method.</returns>
        internal static MethodAccessor CreateAccessor(MethodInfo method) => method.ReturnType == Reflector.VoidType
            ? (MethodAccessor)new ActionMethodAccessor(method)
            : new FunctionMethodAccessor(method);

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Invokes the method. The return value of <see cref="Void"/> methods are <see langword="null"/>.
        /// For static methods the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// </summary>
        /// <param name="instance">The instance that the method belongs to. Can be <see langword="null"/>&#160;for static methods.</param>
        /// <param name="parameters">The parameters to be used for invoking the method.</param>
        /// <returns>The return value of the method, or <see langword="null"/>&#160;for <see cref="Void"/> methods.</returns>
        /// <remarks>
        /// <note>
        /// Invoking the method for the first time is slower than the <see cref="MethodBase.Invoke(object,object[])">System.Reflection.MethodBase.Invoke</see>
        /// method but further calls are much faster.
        /// </note>
        /// <note type="caller">The .NET Standard 2.0 version of this method does not assign back the ref/out parameters in the <paramref name="parameters"/> argument.
        /// Furthermore, if an instance method of a value type (<see langword="struct"/>) mutates the instance,
        /// then the changes will not be applied to the <paramref name="instance"/> parameter in the .NET Standard 2.0 version.
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.InvokeMethod">Reflector.InvokeMethod</see> overloads to invoke methods with ref/out parameters without losing the returned parameter values
        /// and to preserve changes the of the mutated value type instances.</note>
        /// </remarks>
        public abstract object? Invoke(object? instance, params object?[]? parameters);

        #endregion

        #region Private Protected Methods

        /// <summary>
        /// In a derived class returns a delegate that executes the method.
        /// </summary>
        /// <returns>A delegate instance that can be used to invoke the method.</returns>
        private protected abstract Delegate CreateInvoker();

        #endregion

        #endregion

        #endregion
    }
}
