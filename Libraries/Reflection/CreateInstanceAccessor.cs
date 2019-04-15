#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: CreateInstanceAccessor.cs
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
using System.Linq;
using System.Reflection;

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
    ///         const int iterations = 1000000;
    ///         for (int i = 0; i < iterations; i++)
    ///         {
    ///             TestClass result;
    ///             using (Profiler.Measure(GetCategory(i), "Default constructor direct call"))
    ///                 result = new TestClass();
    ///             using (Profiler.Measure(GetCategory(i), "Parameterized constructor direct call"))
    ///                 result = new TestClass(i);
    /// 
    ///             using (Profiler.Measure(GetCategory(i), "CreateInstanceAccessor.CreateInstance (for type)"))
    ///                 result = (TestClass)accessorForType.CreateInstance();
    ///             using (Profiler.Measure(GetCategory(i), "CreateInstanceAccessor.CreateInstance (parameterized constructor)"))
    ///                 result = (TestClass)accessorForCtor.CreateInstance(i);
    /// 
    ///             using (Profiler.Measure(GetCategory(i), "ConstructorInfo.Invoke (default constructor)"))
    ///                 result = (TestClass)ctorDefault.Invoke(null);
    ///             using (Profiler.Measure(GetCategory(i), "ConstructorInfo.Invoke (parameterized constructor)"))
    ///                 result = (TestClass)ctorWithParameters.Invoke(new object[] { i });
    /// 
    ///             using (Profiler.Measure(GetCategory(i), "Activator.CreateInstance (for type)"))
    ///                 result = (TestClass)Activator.CreateInstance(testType);
    ///             using (Profiler.Measure(GetCategory(i), "Activator.CreateInstance (parameterized constructor)"))
    ///                 result = (TestClass)Activator.CreateInstance(testType, i);
    ///         }
    /// 
    ///         string GetCategory(int i) => i < 1 ? "Warm-up" : "Test";
    ///         foreach (IMeasureItem item in Profiler.GetMeasurementResults())
    ///         {
    ///             Console.WriteLine($@"[{item.Category}] {item.Operation}: {item.TotalTime.TotalMilliseconds} ms{(item.NumberOfCalls > 1
    ///                 ? $" (average: {item.TotalTime.TotalMilliseconds / item.NumberOfCalls} ms from {item.NumberOfCalls} calls)" : null)}");
    ///         }
    ///     }
    /// }
    /// 
    /// // This code example produces something like the following output:
    /// // [Warm-up] Default constructor direct call: 0.1471 ms
    /// // [Warm-up] Parameterized constructor direct call: 0.0022 ms
    /// // [Warm-up] CreateInstanceAccessor.CreateInstance (for type): 0.7019 ms
    /// // [Warm-up] CreateInstanceAccessor.CreateInstance (parameterized constructor): 1.8973 ms
    /// // [Warm-up] ConstructorInfo.Invoke (default constructor): 0.0309 ms
    /// // [Warm-up] ConstructorInfo.Invoke (parameterized constructor): 0.0228 ms
    /// // [Warm-up] Activator.CreateInstance (for type): 0.0043 ms
    /// // [Warm-up] Activator.CreateInstance (parameterized constructor): 0.0029 ms
    /// // [Test] Default constructor direct call: 31.9088 ms (average: 3.19088319088319E-05 ms from 999999 calls)
    /// // [Test] Parameterized constructor direct call: 35.113 ms (average: 3.51130351130351E-05 ms from 999999 calls)
    /// // [Test] CreateInstanceAccessor.CreateInstance (for type): 80.0461 ms (average: 8.00461800461801E-05 ms from 999999 calls)
    /// // [Test] CreateInstanceAccessor.CreateInstance (parameterized constructor): 60.5984 ms (average: 6.05984605984606E-05 ms from 999999 calls)
    /// // [Test] ConstructorInfo.Invoke (default constructor): 235.3841 ms (average: 0.000235384335384335 ms from 999999 calls)
    /// // [Test] ConstructorInfo.Invoke (parameterized constructor): 294.9582 ms (average: 0.000294958494958495 ms from 999999 calls)
    /// // [Test] Activator.CreateInstance (for type): 116.7714 ms (average: 0.000116771516771517 ms from 999999 calls)
    /// // [Test] Activator.CreateInstance (parameterized constructor): 814.8384 ms (average: 0.000814839214839215 ms from 999999 calls)]]></code>
    /// </example>
    public abstract class CreateInstanceAccessor : MemberAccessor
    {
        #region Fields

        private Delegate initializer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the instance creator delegate.
        /// </summary>
        internal /*private protected*/ Delegate Initializer => initializer ?? (initializer = CreateInitializer());

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateInstanceAccessor"/> class.
        /// </summary>
        /// <param name="member">Can be a <see cref="Type"/> or a <see cref="ConstructorInfo"/>.</param>
        protected CreateInstanceAccessor(MemberInfo member) :
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
        public static CreateInstanceAccessor GetAccessor(Type type)
            => (CreateInstanceAccessor)GetCreateAccessor(type ?? throw new ArgumentNullException(nameof(type), Res.ArgumentNull));

        /// <summary>
        /// Gets a <see cref="CreateInstanceAccessor"/> for the specified <see cref="ConstructorInfo"/>.
        /// </summary>
        /// <param name="ctor">The constructor for which the accessor should be retrieved.</param>
        /// <returns>A <see cref="CreateInstanceAccessor"/> instance that can be used to create an instance by the constructor.</returns>
        public static CreateInstanceAccessor GetAccessor(ConstructorInfo ctor)
            => (CreateInstanceAccessor)GetCreateAccessor(ctor ?? throw new ArgumentNullException(nameof(ctor), Res.ArgumentNull));

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
                    return new DefaultCreateInstanceAccessor(t);
                default:
                    throw new ArgumentException(Res.ReflectionTypeOrCtorInfoExpected, nameof(member));
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
        /// </remarks>
        public abstract object CreateInstance(params object[] parameters);

        #endregion

        #region Internal Methods

        /// <summary>
        /// In a derived class returns a delegate that creates the new instance.
        /// </summary>
        /// <returns>A delegate instance that can be used to invoke the method.</returns>
        internal /*private protected*/ abstract Delegate CreateInitializer();

        #endregion

        #endregion

        #endregion
    }
}
