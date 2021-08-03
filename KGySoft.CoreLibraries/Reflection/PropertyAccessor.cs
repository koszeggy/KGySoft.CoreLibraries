#region Copyright

///////////////////////////////////////////////////////////////////////////////
//  File: PropertyAccessor.cs
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
    /// Provides an efficient way for setting and getting property values via dynamically created delegates.
    /// <br/>See the <strong>Remarks</strong> section for details and an example.
    /// </summary>
    /// <remarks>
    /// <para>You can obtain a <see cref="PropertyAccessor"/> instance by the static <see cref="GetAccessor">GetAccessor</see> method.</para>
    /// <para>The <see cref="Get">Get</see> and <see cref="Set">Set</see> methods can be used to get and set the property, respectively.
    /// The first call of these methods are slow because the delegates are generated on the first access, but further calls are much faster.</para>
    /// <para>The already obtained accessors are cached so subsequent <see cref="GetAccessor">GetAccessor</see> calls return the already created accessors unless
    /// they were dropped out from the cache, which can store about 8000 elements.</para>
    /// <note>If you want to access a property by name rather then by a <see cref="PropertyInfo"/>, then you can use the <see cref="O:KGySoft.Reflection.Reflector.SetProperty">SetProperty</see>
    /// and <see cref="O:KGySoft.Reflection.Reflector.SetProperty">GetProperty</see> methods in the <see cref="Reflector"/> class, which have some overloads with a <c>propertyName</c> parameter.</note>
    /// <note type="warning">The .NET Standard 2.0 version of the <see cref="Set">Set</see> method throws a <see cref="PlatformNotSupportedException"/>
    /// if the property is an instance member of a value type (<see langword="struct"/>).
    /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
    /// <see cref="O:KGySoft.Reflection.Reflector.SetProperty">Reflector.SetProperty</see> methods to set value type instance properties.</note>
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
    ///         public int TestProperty { get; set; }
    ///     }
    /// 
    ///     static void Main(string[] args)
    ///     {
    ///         var instance = new TestClass();
    ///         PropertyInfo property = instance.GetType().GetProperty(nameof(TestClass.TestProperty));
    ///         PropertyAccessor accessor = PropertyAccessor.GetAccessor(property);
    /// 
    ///         new PerformanceTest { TestName = "Set Property", Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestProperty = 1, "Direct set")
    ///             .AddCase(() => property.SetValue(instance, 1), "PropertyInfo.SetValue")
    ///             .AddCase(() => accessor.Set(instance, 1), "PropertyAccessor.Set")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    /// 
    ///         new PerformanceTest<int> { TestName = "Get Property", Iterations = 1_000_000 }
    ///             .AddCase(() => instance.TestProperty, "Direct get")
    ///             .AddCase(() => (int)property.GetValue(instance), "PropertyInfo.GetValue")
    ///             .AddCase(() => (int)accessor.Get(instance), "PropertyAccessor.Get")
    ///             .DoTest()
    ///             .DumpResults(Console.Out);
    ///     }
    /// }
    /// 
    /// // This code example produces a similar output to this one:
    /// // ==[Set Property Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 3
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct set: average time: 2.93 ms
    /// // 2. PropertyAccessor.Set: average time: 24.22 ms (+21.29 ms / 825.79 %)
    /// // 3. PropertyInfo.SetValue: average time: 214.56 ms (+211.63 ms / 7,314.78 %)
    /// // 
    /// // ==[Get Property Results]================================================
    /// // Iterations: 1,000,000
    /// // Warming up: Yes
    /// // Test cases: 3
    /// // Calling GC.Collect: Yes
    /// // Forced CPU Affinity: No
    /// // Cases are sorted by time (quickest first)
    /// // --------------------------------------------------
    /// // 1. Direct get: average time: 2.58 ms
    /// // 2. PropertyAccessor.Get: average time: 16.62 ms (+14.05 ms / 645.30 %)
    /// // 3. PropertyInfo.GetValue: average time: 169.12 ms (+166.54 ms / 6,564.59 %)]]></code>
    /// </example>
    public abstract class PropertyAccessor : MemberAccessor
    {
        #region Fields

        private Delegate? getter;
        private Delegate? setter;

        #endregion

        #region Properties

        #region Public Properties

        /// <summary>
        /// Gets whether the property can be read (has get accessor).
        /// </summary>
        public bool CanRead => ((PropertyInfo)MemberInfo).CanRead;

        /// <summary>
        /// Gets whether the property can be written (has set accessor).
        /// </summary>
        public bool CanWrite => ((PropertyInfo)MemberInfo).CanWrite;

        #endregion

        #region Private Protected Properties

        /// <summary>
        /// Gets the property getter delegate.
        /// </summary>
        private protected Delegate Getter
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if (getter != null)
                    return getter;
                if (!CanRead)
                    Throw.NotSupportedException(Res.ReflectionPropertyHasNoGetter(MemberInfo.DeclaringType, MemberInfo.Name));
                return getter = CreateGetter();
            }
        }

        /// <summary>
        /// Gets the property setter delegate.
        /// </summary>
        private protected Delegate Setter
        {
            [MethodImpl(MethodImpl.AggressiveInlining)]
            get
            {
                if (setter != null)
                    return setter;
                if (!CanWrite)
                    Throw.NotSupportedException(Res.ReflectionPropertyHasNoSetter(MemberInfo.DeclaringType, MemberInfo.Name));
                return setter = CreateSetter();
            }
        }

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyAccessor"/> class.
        /// </summary>
        /// <param name="property">The property for which the accessor is to be created.</param>
        private protected PropertyAccessor(PropertyInfo property) :
            // ReSharper disable once ConstantConditionalAccessQualifier - null check is in base so it is needed here
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
                ? (PropertyAccessor)new SimplePropertyAccessor(property)
                : new IndexerAccessor(property);

        #endregion

        #endregion

        #region Instance Methods

        #region Public Methods

        /// <summary>
        /// Sets the property.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// If the property is not an indexer, then <paramref name="indexerParameters"/> parameter is omitted.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/>&#160;for static properties.</param>
        /// <param name="value">The value to be set.</param>
        /// <param name="indexerParameters">The parameters if the property is an indexer.</param>
        /// <remarks>
        /// <note>
        /// Setting the property for the first time is slower than the <see cref="PropertyInfo.SetValue(object,object,object[])">System.Reflection.PropertyInfo.SetValue</see>
        /// method but further calls are much faster.
        /// </note>
        /// <note type="caller">Calling the .NET Standard 2.0 version of this method throws a <see cref="PlatformNotSupportedException"/>
        /// if the property is an instance member of a value type (<see langword="struct"/>).
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.SetProperty">Reflector.SetProperty</see> methods to set value type instance properties.</note>
        /// </remarks>
        public abstract void Set(object? instance, object? value, params object?[]? indexerParameters);

        /// <summary>
        /// Gets the value of the property.
        /// For static properties the <paramref name="instance"/> parameter is omitted (can be <see langword="null"/>).
        /// If the property is not an indexer, then <paramref name="indexerParameters"/> parameter is omitted.
        /// </summary>
        /// <param name="instance">The instance that the property belongs to. Can be <see langword="null"/>&#160;for static properties.</param>
        /// <param name="indexerParameters">The parameters if the property is an indexer.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>
        /// <note>
        /// Getting the property for the first time is slower than the <see cref="PropertyInfo.GetValue(object,object[])">System.Reflection.PropertyInfo.GetValue</see>
        /// method but further calls are much faster.
        /// </note>
        /// <note type="caller">When using the .NET Standard 2.0 version of this method, and the getter of an instance property of a value type (<see langword="struct"/>) mutates the instance,
        /// then the changes will not be applied to the <paramref name="instance"/> parameter.
        /// <br/>If you reference the .NET Standard 2.0 version of the <c>KGySoft.CoreLibraries</c> assembly, then use the
        /// <see cref="O:KGySoft.Reflection.Reflector.GetProperty">Reflector.GetProperty</see> methods to preserve changes the of mutated value type instances.</note>
        /// </remarks>
        public abstract object? Get(object? instance, params object?[]? indexerParameters);

        #endregion

        #region Private Protected Methods

        /// <summary>
        /// In a derived class returns a delegate that executes the getter method of the property.
        /// </summary>
        /// <returns>A delegate instance that can be used to get the value of the property.</returns>
        private protected abstract Delegate CreateGetter();

        /// <summary>
        /// In a derived class returns a delegate that executes the setter method of the property.
        /// </summary>
        /// <returns>A delegate instance that can be used to set the property.</returns>
        private protected abstract Delegate CreateSetter();

        #endregion

        #endregion

        #endregion
    }
}
