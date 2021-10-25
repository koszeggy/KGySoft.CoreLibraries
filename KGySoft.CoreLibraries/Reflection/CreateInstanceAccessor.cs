#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CreateInstanceAccessor.cs
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

using KGySoft.CoreLibraries;

#endregion

namespace KGySoft.Reflection
{
    /// <summary>
    /// Provides an efficient way for creating objects via dynamically created delegates.
    /// <br/>See the <strong>Remarks</strong> section for details and an example.
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="CreateInstanceAccessor"/> instance by the static <see cref="O:KGySoft.Reflection.CreateInstanceAccessor.GetAccessor">GetAccessor</see> methods.
    /// There are two overloads of them: <see cref="GetAccessor(Type)"/> can be used for types with parameterless constructors and for creating value types without a constructor,
    /// and the <see cref="GetAccessor(ConstructorInfo)"/> overload is for creating an instance by a specified constructor (with or without parameters).</para>
    /// <para>The <see cref="CreateInstance">CreateInstance</see> method can be used to create an actual instance of an object.
    /// The first call of this method is slow because the delegate is generated on the first access, but further calls are much faster.</para>
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
    ///         ConstructorInfo ctorDefault = testType.GetConstructor(Type.EmptyTypes);
    ///         ConstructorInfo ctorWithParameters = testType.GetConstructor(new[] { typeof(int) });
    ///         CreateInstanceAccessor accessorForType = CreateInstanceAccessor.GetAccessor(testType);
    ///         CreateInstanceAccessor accessorForCtor = CreateInstanceAccessor.GetAccessor(ctorWithParameters);
    /// 
    ///         new PerformanceTest { Iterations = 1_000_000 }
    ///             .AddCase(() => new TestClass(), "Default constructor direct call")
    ///             .AddCase(() => new TestClass(1), "Parameterized constructor direct call")
    ///             .AddCase(() => Activator.CreateInstance(testType), "Activator.CreateInstance by type")
    ///             .AddCase(() => Activator.CreateInstance(testType, 1), "Activator.CreateInstance by constructor parameters")
    ///             .AddCase(() => ctorDefault.Invoke(null), "ConstructorInfo.Invoke (default constructor)")
    ///             .AddCase(() => ctorWithParameters.Invoke(new object[] { 1 }), "ConstructorInfo.Invoke (parameterized constructor)")
    ///             .AddCase(() => accessorForType.CreateInstance(), "CreateInstanceAccessor.CreateInstance (for type)")
    ///             .AddCase(() => accessorForCtor.CreateInstance(1), "CreateInstanceAccessor.CreateInstance (parameterized constructor)")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // ==[Performance Test Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 8
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Default constructor direct call: average time: 4.27 ms
    /// // 2. Parameterized constructor direct call: average time: 4.99 ms (+0.72 ms / 116.89 %)
    /// // 3. CreateInstanceAccessor.CreateInstance (for type): average time: 25.57 ms (+21.30 ms / 599.00 %)
    /// // 4. CreateInstanceAccessor.CreateInstance (parameterized constructor): average time: 26.57 ms (+22.30 ms / 622.28 %)
    /// // 5. Activator.CreateInstance by type: average time: 57.60 ms (+53.33 ms / 1,349.24 %)
    /// // 6. ConstructorInfo.Invoke (default constructor): average time: 163.87 ms (+159.60 ms / 3,838.56 %)
    /// // 7. ConstructorInfo.Invoke (parameterized constructor): average time: 225.14 ms (+220.87 ms / 5,273.79 %)
    /// // 8. Activator.CreateInstance by constructor parameters: average time: 689.54 ms (+685.27 ms / 16,152.22 %)]]></code>
    /// </example>
    public abstract class CreateInstanceAccessor : MemberAccessor
    {
        #region Fields

        private Delegate? initializer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the instance creator delegate.
        /// </summary>
        private protected Delegate Initializer
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get => initializer ??= CreateInitializer();
        }

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
        /// Given <paramref name="type"/> must have a parameterless constructor or type must be <see cref="ValueType"/>.
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
        /// <note>
        /// Invoking the constructor for the first time is slower than the <see cref="MethodBase.Invoke(object,object[])">System.Reflection.MethodBase.Invoke</see>
        /// method but further calls are much faster.
        /// </note>
        /// <note type="caller">The .NET Standard 2.0 version of this method does not assign back the ref/out parameters in the <paramref name="parameters"/> argument.
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.CreateInstance">Reflector.CreateInstance</see> methods to invoke constructors with ref/out parameters without losing the returned parameter values.</note>
        /// </remarks>
        public abstract object CreateInstance(params object?[]? parameters);

        #endregion

        #region Private Protected Methods

        /// <summary>
        /// In a derived class returns a delegate that creates the new instance.
        /// </summary>
        /// <returns>A delegate instance that can be used to invoke the method.</returns>
        private protected abstract Delegate CreateInitializer();

        #endregion

        #endregion

        #endregion
    }
}
